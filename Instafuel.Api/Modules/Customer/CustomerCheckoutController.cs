using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Customer;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/customer")]
public sealed class CustomerCheckoutController(PgDbContext dbContext) : BaseApiController
{
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
        await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var quote = await BuildQuote(request, cancellationToken);
        var order = new Order
        {
            OrderNumber = $"IF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24],
            CustomerId = userId,
            Currency = "BDT",
            Subtotal = quote.Subtotal,
            DeliveryFee = 0m,
            DiscountAmount = 0m,
            TotalAmount = quote.Subtotal,
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
                    Status = "pending"
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
        return CreatedAtAction(nameof(CustomerOrdersController.GetOrder), "CustomerOrders",
            new { orderId = order.Id }, new CustomerOrderSummary(order.Id, order.OrderNumber,
                order.Status, order.Currency, order.TotalAmount, order.CreatedAt));
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
                    && offer.Product.IsActive && offer.Supplier.Status == "active")
                .SelectMany(offer => offer.InventoryBalances
                    .Where(balance => balance.SupplierLocationId == item.SupplierLocationId && balance.SupplierLocation.IsActive
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
        return new CheckoutQuoteResponse("BDT", subtotal, 0m, 0m, subtotal, lines);
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
        if (request.Items.Any(item => item.OfferId == Guid.Empty || item.SupplierLocationId == Guid.Empty || item.Quantity <= 0))
            throw new AppValidationException(nameof(request.Items), "Each checkout item requires a valid offer, location, and positive quantity.");
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
    [property: Required] IReadOnlyList<CheckoutItemRequest> Items);
public sealed record CheckoutItemRequest(Guid OfferId, Guid SupplierLocationId, decimal Quantity);
public sealed record CheckoutQuoteLine(Guid OfferId, Guid SupplierId, Guid SupplierLocationId, string ProductName, string UnitCode, string SupplierName, decimal UnitPrice, decimal Quantity, decimal LineTotal, decimal AvailableQuantity);
public sealed record CheckoutQuoteResponse(string Currency, decimal Subtotal, decimal DeliveryFee, decimal DiscountAmount, decimal TotalAmount, IReadOnlyList<CheckoutQuoteLine> Lines);
