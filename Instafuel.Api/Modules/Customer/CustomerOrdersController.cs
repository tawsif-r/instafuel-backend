using Instafuel.Api.Common.Pagination;
using Instafuel.Api.Authorization;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Customer;

[Authorize(Policy = AuthorizationPolicies.Customer)]
[Route("api/v1/customer")]
public sealed class CustomerOrdersController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<CustomerOrderSummary>>> GetOrders(
        [FromQuery] PagedRequest page, CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var query = dbContext.Orders.AsNoTracking().Where(order => order.CustomerId == userId)
            .Select(order => new CustomerOrderSummary(order.Id, order.OrderNumber, order.Status,
                order.Currency, order.TotalAmount, order.CreatedAt))
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id);
        return Ok(await query.ToPagedResultAsync(page, cancellationToken));
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<CustomerOrderDetails>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        var order = await dbContext.Orders.AsNoTracking()
            .Where(item => item.Id == orderId && item.CustomerId == userId)
            .Select(item => new CustomerOrderDetails(
                item.Id, item.OrderNumber, item.Status, item.Currency, item.Subtotal,
                item.DeliveryFee, item.DiscountAmount, item.TotalAmount, item.RecipientName,
                item.RecipientPhone, item.DeliveryAddress, item.DeliveryCity,
                item.DeliveryLatitude, item.DeliveryLongitude, item.CustomerNotes, item.CreatedAt,
                item.SupplierOrders.Select(supplierOrder => new CustomerSupplierOrder(
                    supplierOrder.Id, supplierOrder.Status,
                    supplierOrder.Items.Select(line => new CustomerOrderLine(
                        line.ProductName, line.SellerName, line.Quantity, line.UnitCode,
                        line.UnitPrice, line.LineTotal)).ToList(),
                    supplierOrder.Deliveries.Select(delivery => new CustomerDelivery(
                        delivery.Id, delivery.Status, delivery.AssignedAt, delivery.PickedUpAt,
                        delivery.DeliveredAt, delivery.RiderLatitude, delivery.RiderLongitude,
                        delivery.LocationUpdatedAt,
                        delivery.StatusHistory.OrderBy(history => history.CreatedAt)
                            .Select(history => new CustomerDeliveryEvent(history.Status, history.Notes, history.CreatedAt)).ToList())).ToList())).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("orders/{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var userId = CustomerControllerSupport.GetUserId(User);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.Orders.Include(item => item.SupplierOrders).ThenInclude(item => item.Deliveries)
            .Include(item => item.Payments)
            .SingleOrDefaultAsync(item => item.Id == orderId && item.CustomerId == userId, cancellationToken);
        if (order is null) return NotFound();
        if (order.Status is not ("pending" or "confirmed"))
            throw new ConflictException("This order can no longer be cancelled.");
        if (order.SupplierOrders.Any(item => item.Status != "pending"))
            throw new ConflictException("Cancellation is unavailable after a supplier starts processing the order.");
        if (order.Payments.Any(payment => payment.Status == "successful"))
            throw new ConflictException("This order has a successful payment and requires the refund workflow before cancellation.");

        var cancellationTime = DateTime.UtcNow;
        var claimed = await dbContext.Orders
            .Where(item => item.Id == order.Id && item.CustomerId == userId
                && (item.Status == "pending" || item.Status == "confirmed")
                && !item.SupplierOrders.Any(supplierOrder => supplierOrder.Status != "pending"))
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.Status, "cancelled")
                .SetProperty(item => item.UpdatedAt, cancellationTime), cancellationToken);
        if (claimed != 1)
            throw new ConflictException("Order status changed and it can no longer be cancelled.");

        var reservations = await dbContext.InventoryMovements.AsNoTracking()
            .Where(movement => movement.ReferenceType == "order" && movement.ReferenceId == order.Id && movement.MovementType == "reservation")
            .Select(movement => new { movement.InventoryBalanceId, movement.QuantityDelta })
            .ToListAsync(cancellationToken);
        foreach (var group in reservations.GroupBy(item => item.InventoryBalanceId))
        {
            var quantity = group.Sum(item => Math.Abs(item.QuantityDelta));
            var updated = await dbContext.InventoryBalances
                .Where(item => item.Id == group.Key && item.QuantityReserved >= quantity)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.QuantityReserved, item => item.QuantityReserved - quantity)
                    .SetProperty(item => item.UpdatedAt, cancellationTime), cancellationToken);
            if (updated != 1)
                throw new ConflictException("The stock reservation has already been released or changed.");
            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                InventoryBalanceId = group.Key, MovementType = "release", QuantityDelta = quantity,
                ReferenceType = "order", ReferenceId = order.Id, Notes = "Customer cancelled order"
            });
        }
        order.Status = "cancelled";
        order.UpdatedAt = cancellationTime;
        foreach (var supplierOrder in order.SupplierOrders)
        {
            supplierOrder.Status = "cancelled";
            supplierOrder.UpdatedAt = cancellationTime;
            dbContext.SupplierOrderStatusHistories.Add(new SupplierOrderStatusHistory
            {
                SupplierOrderId = supplierOrder.Id,
                Status = "cancelled",
                Notes = "Customer cancelled order",
                RecordedByUserId = userId,
                CreatedAt = cancellationTime
            });
            foreach (var delivery in supplierOrder.Deliveries.Where(item => item.Status == "pending"))
            {
                delivery.Status = "cancelled";
                dbContext.DeliveryStatusHistories.Add(new DeliveryStatusHistory
                {
                    DeliveryId = delivery.Id,
                    Status = "cancelled",
                    Notes = "Customer cancelled order",
                    RecordedByUserId = userId,
                    CreatedAt = cancellationTime
                });
            }
        }
        foreach (var payment in order.Payments.Where(payment => payment.Status == "pending"))
            payment.Status = "cancelled";
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record CustomerOrderSummary(Guid Id, string OrderNumber, string Status, string Currency, decimal TotalAmount, DateTime CreatedAt);
public sealed record CustomerOrderDetails(Guid Id, string OrderNumber, string Status, string Currency, decimal Subtotal, decimal DeliveryFee, decimal DiscountAmount, decimal TotalAmount, string RecipientName, string RecipientPhone, string DeliveryAddress, string DeliveryCity, decimal? DeliveryLatitude, decimal? DeliveryLongitude, string? CustomerNotes, DateTime CreatedAt, IReadOnlyList<CustomerSupplierOrder> SupplierOrders);
public sealed record CustomerSupplierOrder(Guid Id, string Status, IReadOnlyList<CustomerOrderLine> Items, IReadOnlyList<CustomerDelivery> Deliveries);
public sealed record CustomerOrderLine(string ProductName, string SellerName, decimal Quantity, string UnitCode, decimal UnitPrice, decimal LineTotal);
public sealed record CustomerDelivery(Guid Id, string Status, DateTime? AssignedAt, DateTime? PickedUpAt,
    DateTime? DeliveredAt, decimal? RiderLatitude, decimal? RiderLongitude, DateTime? LocationUpdatedAt,
    IReadOnlyList<CustomerDeliveryEvent> History);
public sealed record CustomerDeliveryEvent(string Status, string? Notes, DateTime CreatedAt);
