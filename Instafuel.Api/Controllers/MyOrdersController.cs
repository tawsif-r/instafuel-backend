using Instafuel.Api.Dtos;
using Instafuel.Api.Extensions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

// The logged-in customer's own orders. Every query is filtered by the user id
// from the token, so another customer's order id simply returns 404.
//
// An order is a list of lines, one per customer vehicle (vehicle + fuel + litres).
// Each fuel type is priced with the cheapest active supplier offer, and lines are
// grouped into one SupplierOrder per supplier, like OrdersController.CreateOrder.
//
// TODO: stock checks, delivery zones/fees and corporate billing (see docs/architecture.md §7).
[ApiController]
[Authorize]
[Route("api/me/orders")]
public class MyOrdersController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<MyOrdersController> _logger;

    public MyOrdersController(PgDbContext context, IConfiguration config, ILogger<MyOrdersController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    // Price per litre of each fuel type that can be ordered right now.
    [HttpGet("fuel-products")]
    public async Task<ActionResult<IEnumerable<FuelProductResponse>>> GetFuelProducts()
    {
        try
        {
            var offers = await _context.CheapestFuelOffersAsync();
            return Ok(FuelOfferQueries.FuelTypes
                .Where(offers.ContainsKey)
                .Select(t => new FuelProductResponse(t, offers[t].UnitPrice)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve fuel products. Operation: {Operation}", nameof(GetFuelProducts));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving fuel prices." });
        }
    }

    // Price preview before the customer confirms. Uses the same rules as PlaceOrder.
    [HttpPost("quote")]
    public async Task<ActionResult<OrderQuoteResponse>> Quote(QuoteOrderRequest request)
    {
        try
        {
            var (lines, error) = await PriceLines(request.Lines);
            if (error is not null) return BadRequest(new { message = error });

            var subtotal = lines.Sum(l => l.LineTotal);
            var fee = DeliveryFee();
            return Ok(new OrderQuoteResponse(subtotal, fee, subtotal + fee));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to quote order. Operation: {Operation}", nameof(Quote));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while calculating the price." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MyOrderResponse>>> GetMyOrders()
    {
        try
        {
            var orders = await MyOrders().AsNoTracking().OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Ok(orders.Select(MyOrderResponse.From));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer orders. Operation: {Operation}", nameof(GetMyOrders));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving your orders." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MyOrderResponse>> GetMyOrder(Guid id)
    {
        try
        {
            var order = await MyOrders().AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            return order is null ? NotFound() : Ok(MyOrderResponse.From(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer order {OrderId}. Operation: {Operation}", id, nameof(GetMyOrder));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the order." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MyOrderResponse>> PlaceOrder(PlaceOrderRequest request)
    {
        try
        {
            var leadHours = _config.GetValue("Orders:MinLeadTimeHours", 24);
            var requestedAt = request.RequestedDeliveryAt.ToUniversalTime();
            if (requestedAt < DateTime.UtcNow.AddHours(leadHours))
                return BadRequest(new { message = $"Delivery must be at least {leadHours} hours from now." });

            var customer = await _context.Users.FindAsync(User.GetUserId());
            if (customer is null || !customer.IsActive)
                return BadRequest(new { message = "Your account is not active." });

            var (lines, error) = await PriceLines(request.Lines);
            if (error is not null) return BadRequest(new { message = error });

            var order = new Order
            {
                OrderNumber = $"IF-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                CustomerId = customer.Id,
                RecipientName = customer.FullName,
                RecipientPhone = customer.Phone ?? string.Empty,
                DeliveryAddress = request.DeliveryAddress.Trim(),
                // TODO: the app sends a single address string; ask for the city once delivery zones exist.
                DeliveryCity = _config.GetValue("Orders:DefaultCity", "Dhaka")!,
                CustomerNotes = string.IsNullOrWhiteSpace(request.CustomerNotes) ? null : request.CustomerNotes.Trim(),
                RequestedDeliveryAt = requestedAt
            };

            // One SupplierOrder per supplier, fulfilled by its first active location.
            foreach (var group in lines.GroupBy(l => l.Offer.SupplierId))
            {
                var supplier = group.First().Offer.Supplier;
                var supplierOrder = new SupplierOrder
                {
                    SupplierId = supplier.Id,
                    SupplierLocationId = supplier.Locations.First(l => l.IsActive).Id
                };

                foreach (var line in group)
                {
                    supplierOrder.Items.Add(new OrderItem
                    {
                        SupplierOfferId = line.Offer.Id,
                        CustomerVehicleId = line.Vehicle.Id,
                        ProductName = line.Offer.Product.Name,
                        SellerName = supplier.DisplayName,
                        VehicleName = line.Vehicle.Nickname,
                        VehicleRegistration = line.Vehicle.RegistrationNumber,
                        FuelType = line.FuelType,
                        Quantity = line.Quantity,
                        UnitCode = line.Offer.Product.UnitCode,
                        UnitPrice = line.Offer.UnitPrice,
                        LineTotal = line.LineTotal
                    });
                }

                supplierOrder.Subtotal = supplierOrder.Items.Sum(i => i.LineTotal);
                order.SupplierOrders.Add(supplierOrder);
            }

            order.Subtotal = order.SupplierOrders.Sum(s => s.Subtotal);
            order.DeliveryFee = DeliveryFee();
            order.DiscountAmount = 0;
            order.TotalAmount = order.Subtotal + order.DeliveryFee - order.DiscountAmount;

            // A single SaveChanges runs in one DB transaction: all rows are saved, or none.
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMyOrder), new { id = order.Id }, MyOrderResponse.From(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place customer order. Operation: {Operation}", nameof(PlaceOrder));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while placing the order." });
        }
    }

    // Allowed until a driver is assigned.
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<MyOrderResponse>> CancelMyOrder(Guid id)
    {
        try
        {
            var order = await MyOrders().FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return NotFound();

            var deliveries = order.SupplierOrders.SelectMany(s => s.Deliveries).ToList();
            if (order.Status is not ("pending" or "confirmed") || deliveries.Any(d => d.Status != "pending"))
                return BadRequest(new { message = "This order can no longer be cancelled." });

            var now = DateTime.UtcNow;
            order.Status = "cancelled";
            order.UpdatedAt = now;
            foreach (var supplierOrder in order.SupplierOrders)
            {
                supplierOrder.Status = "cancelled";
                supplierOrder.UpdatedAt = now;
            }
            foreach (var delivery in deliveries)
            {
                delivery.Status = "cancelled";
                delivery.StatusHistory.Add(new DeliveryStatusHistory
                {
                    Status = "cancelled",
                    Notes = "Order cancelled by the customer",
                    RecordedByUserId = order.CustomerId
                });
            }

            // Nothing was collected yet for cash/bank transfer, so drop those.
            // Corporate credit payments restore the used credit limit upon cancellation.
            // For any successful payment (Card, Mobile Banking, Corporate Credit), create a Refund record.
            foreach (var payment in order.Payments)
            {
                if (payment.Status == "successful")
                {
                    if (payment.Method == "corporate_credit")
                    {
                        var corp = await _context.CompanyMembers
                            .Include(m => m.Company)
                            .FirstOrDefaultAsync(m => m.UserId == order.CustomerId && m.Status == "active");
                        if (corp is not null)
                        {
                            corp.UsedCredit = Math.Max(0, corp.UsedCredit - payment.Amount);
                            corp.Company.UsedCredit = Math.Max(0, corp.Company.UsedCredit - payment.Amount);
                        }
                    }

                    payment.Status = "refunded";

                    var refund = new Refund
                    {
                        PaymentId = payment.Id,
                        Amount = payment.Amount,
                        Status = "successful",
                        ProviderReference = payment.Method == "corporate_credit"
                            ? $"REF-CORP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
                            : $"REF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        CreatedAt = now
                    };
                    _context.Refunds.Add(refund);
                    payment.Refunds.Add(refund);
                }
                else if (payment.Status == "pending")
                {
                    payment.Status = "cancelled";
                }
            }

            await _context.SaveChangesAsync();
            return Ok(MyOrderResponse.From(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel customer order {OrderId}. Operation: {Operation}", id, nameof(CancelMyOrder));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while cancelling the order." });
        }
    }

    // Cash and bank transfer stay "pending" until the driver/finance confirms.
    // Card and mobile banking need a payment gateway, which doesn't exist yet: with
    // Payments:SimulateGateway (Development) they succeed straight away, otherwise
    // they stay "pending".
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PayOrderResponse>> PayMyOrder(Guid id, PayOrderRequest request)
    {
        try
        {
            var order = await MyOrders().FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return NotFound();

            // Same idempotency key = the app retried the same request, so return the original payment.
            var existing = order.Payments.FirstOrDefault(p => p.IdempotencyKey == request.IdempotencyKey);
            if (existing is not null) return Ok(ToPayResponse(existing));
            if (await _context.Payments.AnyAsync(p => p.IdempotencyKey == request.IdempotencyKey))
                return Conflict(new { message = "This payment key was already used." });

            if (order.Status is "cancelled" or "completed" or "failed")
                return BadRequest(new { message = $"Order is {order.Status}." });
            if (order.Payments.Any(p => p.Status is "pending" or "successful"))
                return BadRequest(new { message = "This order already has a payment." });

            CompanyMember? corporateMembership = null;
            if (request.Method == "corporate_credit")
            {
                corporateMembership = await _context.CompanyMembers
                    .Include(m => m.Company)
                    .FirstOrDefaultAsync(m => m.UserId == order.CustomerId && m.Status == "active" && m.Company.IsActive);

                if (corporateMembership is null)
                    return BadRequest(new { message = "You do not have an active corporate account." });

                var availableCredit = corporateMembership.CreditLimit - corporateMembership.UsedCredit;
                if (availableCredit < order.TotalAmount)
                    return BadRequest(new { message = $"Insufficient corporate credit limit. Available: {availableCredit:N2} BDT." });

                corporateMembership.UsedCredit += order.TotalAmount;
                corporateMembership.Company.UsedCredit += order.TotalAmount;
            }

            var simulate = _config.GetValue("Payments:SimulateGateway", false);
            var isCorporate = request.Method == "corporate_credit";
            var instant = (request.Method is "card" or "mobile_banking" && simulate) || isCorporate;
            var payment = new Payment
            {
                Amount = order.TotalAmount,
                Currency = order.Currency,
                Method = request.Method,
                IdempotencyKey = request.IdempotencyKey,
                Status = instant ? "successful" : "pending",
                PaidAt = instant ? DateTime.UtcNow : null,
                ProviderReference = isCorporate ? $"CORP-{corporateMembership!.Company.Code}" : (instant ? $"SIM-{Guid.NewGuid().ToString("N")[..10].ToUpper()}" : null)
            };
            order.Payments.Add(payment);

            if (order.Status == "pending")
            {
                order.Status = "confirmed";
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(ToPayResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pay customer order {OrderId}. Operation: {Operation}", id, nameof(PayMyOrder));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while processing the payment." });
        }
    }

    // Orders of the logged-in user with everything MyOrderResponse.From needs.
    private IQueryable<Order> MyOrders()
    {
        var userId = User.GetUserId();
        return _context.Orders
            .Include(o => o.SupplierOrders).ThenInclude(s => s.Items)
            .Include(o => o.SupplierOrders).ThenInclude(s => s.Deliveries).ThenInclude(d => d.Driver!).ThenInclude(d => d.User)
            .Include(o => o.SupplierOrders).ThenInclude(s => s.Deliveries).ThenInclude(d => d.Vehicle)
            .Include(o => o.SupplierOrders).ThenInclude(s => s.Deliveries).ThenInclude(d => d.StatusHistory)
            .Include(o => o.Payments).ThenInclude(p => p.Refunds)
            .AsSplitQuery()
            .Where(o => o.CustomerId == userId);
    }

    private decimal DeliveryFee() => _config.GetValue("Orders:DeliveryFee", 0m);

    private record PricedLine(CustomerVehicle Vehicle, SupplierOffer Offer, string FuelType, decimal Quantity, decimal LineTotal);

    // Checks the lines (own verified vehicles, each once, fuel available) and prices them.
    // Enforces the 20-litre minimum total order volume per SOP §5.4.
    // Returns an error message that is safe to show the customer when something is wrong.
    private async Task<(List<PricedLine> Lines, string? Error)> PriceLines(List<OrderLineRequest> requestLines)
    {
        if (requestLines.Count == 0)
            return ([], "At least one vehicle line is required.");

        if (requestLines.Select(l => l.CustomerVehicleId).Distinct().Count() != requestLines.Count)
            return ([], "Each vehicle can appear only once in an order.");

        if (requestLines.Any(l => l.Quantity <= 0))
            return ([], "Quantity must be greater than zero.");

        var minLitres = _config.GetValue<decimal>("Orders:MinOrderLitres", 20.0m);
        var totalLitres = requestLines.Sum(l => l.Quantity);
        if (totalLitres < minLitres)
            return ([], $"Minimum total order volume is {minLitres:0.#} litres (SOP §5.4). Current order: {totalLitres:0.#} litres.");

        var userId = User.GetUserId();
        var vehicleIds = requestLines.Select(l => l.CustomerVehicleId).ToList();
        var vehicles = await _context.CustomerVehicles.AsNoTracking()
            .Where(v => vehicleIds.Contains(v.Id) && v.UserId == userId && v.IsActive)
            .ToDictionaryAsync(v => v.Id);
        var offers = await _context.CheapestFuelOffersAsync();

        var lines = new List<PricedLine>();
        foreach (var line in requestLines)
        {
            if (!vehicles.TryGetValue(line.CustomerVehicleId, out var vehicle))
                return ([], "Vehicle not found.");
            if (vehicle.VerificationStatus != "verified")
                return ([], $"{vehicle.Nickname} is not verified yet. Only verified vehicles can be ordered for.");
            if (!offers.TryGetValue(line.FuelType, out var offer))
                return ([], $"{char.ToUpper(line.FuelType[0])}{line.FuelType[1..]} is not available right now.");

            lines.Add(new PricedLine(vehicle, offer, line.FuelType, line.Quantity, Math.Round(offer.UnitPrice * line.Quantity, 2)));
        }
        return (lines, null);
    }

    private static PayOrderResponse ToPayResponse(Payment payment) =>
        new(payment.OrderId, payment.Status, payment.Method, payment.Amount, payment.ProviderReference, null);
}
