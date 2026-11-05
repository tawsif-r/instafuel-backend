using Instafuel.Api.Dtos;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(PgDbContext context, ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/orders?customerId=...
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders([FromQuery] Guid? customerId)
    {
        try
        {
            var query = _context.Orders.AsNoTracking();
            if (customerId is not null)
                query = query.Where(o => o.CustomerId == customerId);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve orders. Operation: {Operation}", nameof(GetOrders));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving orders." });
        }
    }

    // Returns the order with its supplier orders, their items and payments.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        try
        {
            var order = await _context.Orders.AsNoTracking()
                .Include(o => o.SupplierOrders).ThenInclude(s => s.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);
            return order is null ? NotFound() : Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve order {OrderId}. Operation: {Operation}", id, nameof(GetOrder));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the order." });
        }
    }

    // Creates an order from a list of supplier offers.
    // Items are grouped into one SupplierOrder per supplier, and prices/names are
    // copied from the offer at this moment (so later price changes don't affect the order).
    //
    // TODO: 24h lead time, stock checks and delivery fee rules (see docs/plan.md)
    // are not implemented yet.
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderRequest request)
    {
        try
        {
            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer is null || !customer.IsActive)
                return BadRequest(new { message = "Customer not found or inactive." });

            if (request.Items.Select(i => i.SupplierOfferId).Distinct().Count() != request.Items.Count)
                return BadRequest(new { message = "Each offer can appear only once in an order." });

            var offerIds = request.Items.Select(i => i.SupplierOfferId).ToList();
            var offers = await _context.SupplierOffers
                .Include(o => o.Product)
                .Include(o => o.Supplier).ThenInclude(s => s.Locations)
                .Where(o => offerIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id);

            foreach (var item in request.Items)
            {
                if (!offers.TryGetValue(item.SupplierOfferId, out var offer))
                    return BadRequest(new { message = $"Offer {item.SupplierOfferId} not found." });
                if (offer.Status != "active" || offer.Supplier.Status != "active" || !offer.Product.IsActive)
                    return BadRequest(new { message = $"Offer {item.SupplierOfferId} is not available." });
            }

            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                CustomerId = request.CustomerId,
                RecipientName = request.RecipientName,
                RecipientPhone = request.RecipientPhone,
                DeliveryAddress = request.DeliveryAddress,
                DeliveryCity = request.DeliveryCity,
                DeliveryLatitude = request.DeliveryLatitude,
                DeliveryLongitude = request.DeliveryLongitude,
                CustomerNotes = request.CustomerNotes
            };

            // One SupplierOrder per supplier.
            foreach (var group in request.Items.GroupBy(i => offers[i.SupplierOfferId].SupplierId))
            {
                var supplier = offers[group.First().SupplierOfferId].Supplier;

                // For now the supplier's first active location fulfils the order.
                var location = supplier.Locations.FirstOrDefault(l => l.IsActive);
                if (location is null)
                    return BadRequest(new { message = $"Supplier {supplier.DisplayName} has no active location." });

                var supplierOrder = new SupplierOrder
                {
                    SupplierId = supplier.Id,
                    SupplierLocationId = location.Id
                };

                foreach (var item in group)
                {
                    var offer = offers[item.SupplierOfferId];
                    supplierOrder.Items.Add(new OrderItem
                    {
                        SupplierOfferId = offer.Id,
                        ProductName = offer.Product.Name,
                        SellerName = supplier.DisplayName,
                        Quantity = item.Quantity,
                        UnitCode = offer.Product.UnitCode,
                        UnitPrice = offer.UnitPrice,
                        LineTotal = Math.Round(offer.UnitPrice * item.Quantity, 2)
                    });
                }

                supplierOrder.Subtotal = supplierOrder.Items.Sum(i => i.LineTotal);
                order.SupplierOrders.Add(supplierOrder);
            }

            order.Subtotal = order.SupplierOrders.Sum(s => s.Subtotal);
            order.DeliveryFee = 0;    // delivery fee rules are not decided yet
            order.DiscountAmount = 0;
            order.TotalAmount = order.Subtotal + order.DeliveryFee - order.DiscountAmount;

            // A single SaveChanges runs in one DB transaction: all rows are saved, or none.
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order. Operation: {Operation}", nameof(CreateOrder));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the order." });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Order>> UpdateOrderStatus(Guid id, UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order is null) return NotFound();

            if (order.Status is "completed" or "cancelled")
                return BadRequest(new { message = $"Order is already {order.Status}." });

            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of order {OrderId}. Operation: {Operation}", id, nameof(UpdateOrderStatus));
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the order status." });
        }
    }
}
