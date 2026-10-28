using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Customer;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customer")]
public sealed class CustomerCheckoutController(PgDbContext dbContext, IConfiguration configuration) : BaseApiController
{
    private const decimal MaxOrderAmount = 999999999999.99m;
    [HttpPost("checkout/quote")]
    public async Task<ActionResult<CheckoutQuoteResponse>> Quote(CheckoutRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var quote = await BuildQuote(request, cancellationToken);
        return Ok(quote);
    }

    [HttpPost("orders")]
    public async Task<ActionResult<CustomerOrderSummary>> PlaceOrder(CheckoutRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var userId = CustomerControllerSupport.GetUserId(User);
        var idempotencyKey = request.IdempotencyKey.Trim();
        var fingerprint = Fingerprint(request);
        var existingOrder = await dbContext.Orders.AsNoTracking()
            .SingleOrDefaultAsync(order => order.CustomerId == userId
                && order.CheckoutIdempotencyKey == idempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.CheckoutRequestFingerprint != fingerprint)
                throw new ConflictException("This checkout idempotency key was already used with different order details.");
            return Created($"/api/v1/customer/orders/{existingOrder.Id}", new CustomerOrderSummary(
                existingOrder.Id, existingOrder.OrderNumber, existingOrder.Status, existingOrder.Currency,
                existingOrder.TotalAmount, existingOrder.CreatedAt));
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var quote = await BuildQuote(request, cancellationToken);
        var order = new Order
        {
            OrderNumber = $"IF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}",
            CustomerId = userId,
            CheckoutIdempotencyKey = idempotencyKey,
            CheckoutRequestFingerprint = fingerprint,
            Currency = "BDT",
            Subtotal = quote.Subtotal,
            DeliveryFee = quote.DeliveryFee,
            DiscountAmount = 0m,
            TotalAmount = quote.TotalAmount,
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            DeliveryCity = request.City.Trim(),
            DeliveryLatitude = request.Latitude,
            DeliveryLongitude = request.Longitude,
            CustomerNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        var groups = new Dictionary<(Guid SupplierId, Guid LocationId), SupplierOrder>();
        foreach (var line in quote.Lines)
        {
            var reservation = await dbContext.InventoryBalances
                .Where(balance => balance.SupplierLocationId == line.SupplierLocationId && balance.SupplierOfferId == line.OfferId
                    && balance.SupplierLocation.IsActive && balance.SupplierLocation.SupplierId == line.SupplierId
                    && balance.SupplierLocation.City.ToLower() == request.City.Trim().ToLower()
                    && balance.SupplierOffer.Status == "active" && balance.SupplierOffer.Currency == "BDT"
                    && balance.SupplierOffer.Product.IsActive && balance.SupplierOffer.Product.Category.IsActive
                    && balance.SupplierOffer.Supplier.Status == "active"
                    && balance.QuantityOnHand - balance.QuantityReserved >= line.Quantity)
                .ExecuteUpdateAsync(update => update.SetProperty(balance => balance.QuantityReserved,
                    balance => balance.QuantityReserved + line.Quantity), cancellationToken);
            if (reservation != 1) throw new ConflictException("Requested stock is no longer available. Please request a new quote.");

            if (!groups.TryGetValue((line.SupplierId, line.SupplierLocationId), out var supplierOrder))
            {
                supplierOrder = new SupplierOrder
                {
                    SupplierId = line.SupplierId,
                    SupplierLocationId = line.SupplierLocationId,
                    Status = "pending",
                    StatusHistory =
                    [
                        new SupplierOrderStatusHistory
                        {
                            Status = "pending",
                            Notes = "Supplier order created at checkout",
                            RecordedByUserId = userId
                        }
                    ],
                    Deliveries =
                    [
                        new Delivery
                        {
                            StatusHistory =
                            [
                                new DeliveryStatusHistory
                                {
                                    Status = "pending",
                                    Notes = "Delivery created for supplier order",
                                    RecordedByUserId = userId
                                }
                            ]
                        }
                    ]
                };
                groups.Add((line.SupplierId, line.SupplierLocationId), supplierOrder);
            }
            supplierOrder.Subtotal += line.LineTotal;
            supplierOrder.Items.Add(new OrderItem
            {
                SupplierOfferId = line.OfferId,
                ProductName = line.ProductName,
                SellerName = line.SupplierName,
                Quantity = line.Quantity,
                UnitCode = line.UnitCode,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotal
            });
        }
        order.SupplierOrders = groups.Values.ToList();
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        // This API currently supports COD only. Create its payment intent atomically with
        // the order so delivery completion can never bypass the payment collection check.
        dbContext.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = order.Currency,
            Method = "cash",
            Status = "pending",
            IdempotencyKey = $"checkout:{order.Id:N}"
        });

        foreach (var line in quote.Lines)
        {
            var balanceId = await dbContext.InventoryBalances.AsNoTracking()
                .Where(balance => balance.SupplierLocationId == line.SupplierLocationId && balance.SupplierOfferId == line.OfferId)
                .Select(balance => balance.Id).SingleAsync(cancellationToken);
            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                InventoryBalanceId = balanceId, MovementType = "reservation", QuantityDelta = line.Quantity,
                ReferenceType = "order", ReferenceId = order.Id, Notes = "Customer checkout reservation"
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Created($"/api/v1/customer/orders/{order.Id}", new CustomerOrderSummary(
            order.Id, order.OrderNumber, order.Status, order.Currency, order.TotalAmount, order.CreatedAt));
    }

    private async Task<CheckoutQuoteResponse> BuildQuote(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var requested = request.Items.GroupBy(item => (item.OfferId, item.SupplierLocationId))
            .Select(group => new { group.Key.OfferId, group.Key.SupplierLocationId, Quantity = group.Sum(item => item.Quantity) }).ToList();
        if (requested.Any(item => item.Quantity <= 0 || item.Quantity > 10000))
            throw new AppValidationException(nameof(request.Items), "Item quantity must be greater than zero and at most 10,000.");

        var lines = new List<CheckoutQuoteLine>();
        foreach (var item in requested)
        {
            var line = await dbContext.SupplierOffers.AsNoTracking()
                .Where(offer => offer.Id == item.OfferId && offer.Status == "active" && offer.Currency == "BDT"
                    && offer.Product.IsActive && offer.Product.Category.IsActive && offer.Supplier.Status == "active")
                .SelectMany(offer => offer.InventoryBalances
                    .Where(balance => balance.SupplierLocationId == item.SupplierLocationId && balance.SupplierLocation.IsActive
                        && balance.SupplierLocation.SupplierId == offer.SupplierId
                        && balance.SupplierLocation.City.ToLower() == request.City.Trim().ToLower()
                        && balance.QuantityOnHand - balance.QuantityReserved >= item.Quantity)
                    .Select(balance => new CheckoutQuoteLine(offer.Id, offer.SupplierId, balance.SupplierLocationId,
                        offer.Product.Name, offer.Product.UnitCode, offer.Supplier.DisplayName,
                        offer.UnitPrice, item.Quantity, decimal.Round(offer.UnitPrice * item.Quantity, 2),
                        balance.QuantityOnHand - balance.QuantityReserved)))
                .SingleOrDefaultAsync(cancellationToken);
            if (line is null) throw new ConflictException("One or more offers are inactive or have insufficient stock.");
            lines.Add(line);
        }
        var subtotal = decimal.Round(lines.Sum(line => line.LineTotal), 2);
        if (lines.Any(line => line.LineTotal > MaxOrderAmount) || subtotal > MaxOrderAmount)
            throw new AppValidationException(nameof(request.Items), "Checkout total exceeds the supported amount limit.");
        var deliveryFee = configuration.GetValue<decimal?>("Checkout:DeliveryFeeBdt")
            ?? throw new InvalidOperationException("Checkout:DeliveryFeeBdt must be configured before accepting orders.");
        if (deliveryFee < 0 || deliveryFee > MaxOrderAmount)
            throw new InvalidOperationException("Checkout:DeliveryFeeBdt must be a valid non-negative amount.");
        var total = decimal.Round(subtotal + deliveryFee, 2);
        if (total <= 0)
            throw new AppValidationException(nameof(request.Items), "Checkout total must be greater than zero.");
        if (total > MaxOrderAmount)
            throw new AppValidationException(nameof(request.Items), "Checkout total exceeds the supported amount limit.");
        return new CheckoutQuoteResponse("BDT", subtotal, deliveryFee, 0m, total, lines);
    }

    private static void Validate(CheckoutRequest request)
    {
        if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 50)
            throw new AppValidationException(nameof(request.Items), "Checkout must contain between 1 and 50 items.");
        if (new[] { request.RecipientName, request.RecipientPhone, request.DeliveryAddress, request.City }.Any(string.IsNullOrWhiteSpace))
            throw new AppValidationException("Delivery recipient, phone, address, and city are required.");
        if (request.RecipientName.Length > 150 || request.RecipientPhone.Length > 30 || request.DeliveryAddress.Length > 500 || request.City.Length > 100 || request.Notes?.Length > 1000)
            throw new AppValidationException("One or more checkout fields exceed their maximum length.");
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            throw new AppValidationException("Coordinates are outside valid latitude or longitude ranges.");
        if (request.Latitude.HasValue != request.Longitude.HasValue)
            throw new AppValidationException("Latitude and longitude must either both be provided or both be empty.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 200)
            throw new AppValidationException(nameof(request.IdempotencyKey), "A checkout idempotency key up to 200 characters is required.");
        if (request.Items.Any(item => item.OfferId == Guid.Empty || item.SupplierLocationId == Guid.Empty
                || item.Quantity <= 0 || item.Quantity != decimal.Round(item.Quantity, 3)))
            throw new AppValidationException(nameof(request.Items), "Each checkout item requires a valid offer and location, and a positive quantity with at most 3 decimal places.");
    }

    private static string Fingerprint(CheckoutRequest request)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            request.RecipientName,
            request.RecipientPhone,
            request.DeliveryAddress,
            City = request.City.Trim().ToUpperInvariant(),
            request.Latitude,
            request.Longitude,
            Notes = request.Notes?.Trim(),
            Items = request.Items.OrderBy(item => item.OfferId).ThenBy(item => item.SupplierLocationId)
                .ThenBy(item => item.Quantity).ToArray()
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public sealed record CheckoutRequest(
    [property: Required, StringLength(150)] string RecipientName,
    [property: Required, StringLength(30)] string RecipientPhone,
    [property: Required, StringLength(500)] string DeliveryAddress,
    [property: Required, StringLength(100)] string City,
    decimal? Latitude,
    decimal? Longitude,
    [property: StringLength(1000)] string? Notes,
    [property: Required] IReadOnlyList<CheckoutItemRequest> Items,
    [property: Required, StringLength(200, MinimumLength = 1)] string IdempotencyKey);
public sealed record CheckoutItemRequest(Guid OfferId, Guid SupplierLocationId, decimal Quantity);
public sealed record CheckoutQuoteLine(Guid OfferId, Guid SupplierId, Guid SupplierLocationId, string ProductName, string UnitCode, string SupplierName, decimal UnitPrice, decimal Quantity, decimal LineTotal, decimal AvailableQuantity);
public sealed record CheckoutQuoteResponse(string Currency, decimal Subtotal, decimal DeliveryFee, decimal DiscountAmount, decimal TotalAmount, IReadOnlyList<CheckoutQuoteLine> Lines);
