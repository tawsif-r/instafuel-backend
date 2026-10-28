using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Common.Pagination;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Suppliers;

[Authorize]
[Route("api/v1/supplier/{supplierId:guid}/orders")]
public sealed class SupplierOrdersController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierOrderResponse>>> GetOrders(Guid supplierId,
        [FromQuery] PagedRequest page, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(status)
            && status is not ("pending" or "accepted" or "rejected" or "preparing" or "ready" or "dispatched" or "delivered" or "cancelled"))
            throw new AppValidationException(nameof(status), "Unsupported supplier order status filter.");

        var query = dbContext.SupplierOrders.AsNoTracking().Where(item => item.SupplierId == supplierId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status);
        var projected = query.Select(item => new SupplierOrderResponse(item.Id, item.Order.OrderNumber, item.Status,
                item.SupplierLocationId, item.SupplierLocation.Name, item.Subtotal, item.Order.Currency,
                item.Order.RecipientName, item.Order.RecipientPhone, item.Order.DeliveryAddress,
                item.Order.DeliveryCity, item.Order.CustomerNotes, item.CreatedAt,
                item.Items.OrderBy(line => line.CreatedAt).Select(line => new SupplierOrderLineResponse(
                    line.Id, line.ProductName, line.Quantity, line.UnitCode, line.UnitPrice, line.LineTotal)).ToList(),
                item.StatusHistory.OrderBy(history => history.CreatedAt).Select(history =>
                    new SupplierOrderEvent(history.Status, history.Notes, history.CreatedAt)).ToList()))
            .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id);
        return Ok(await projected.ToPagedResultAsync(page, cancellationToken));
    }

    [HttpPut("{supplierOrderId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid supplierId, Guid supplierOrderId,
        SupplierOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not ("accepted" or "rejected" or "preparing" or "ready"))
            throw new AppValidationException(nameof(request.Status), "Supplier may set accepted, rejected, preparing, or ready.");
        if (request.Notes?.Length > 1000) throw new AppValidationException(nameof(request.Notes), "Notes must be at most 1000 characters.");
        var userId = SupplierControllerSupport.GetUserId(User);
        await SupplierControllerSupport.RequireMemberAsync(dbContext, supplierId, userId, cancellationToken,
            "owner", "manager", "staff");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var supplierOrder = await dbContext.SupplierOrders.AsNoTracking()
            .Where(item => item.Id == supplierOrderId && item.SupplierId == supplierId)
            .Select(item => new { item.Id, item.Status, item.OrderId, item.SupplierLocationId,
                Items = item.Items.Select(line => new { line.SupplierOfferId, line.Quantity }).ToList(),
                DeliveryIds = item.Deliveries.Select(delivery => delivery.Id).ToList() })
            .SingleOrDefaultAsync(cancellationToken);
        if (supplierOrder is null) return NotFound();

        if (!SupplierOrderWorkflow.CanTransition(supplierOrder.Status, request.Status))
            throw new ConflictException($"Supplier order cannot move from '{supplierOrder.Status}' to '{request.Status}'.");

        var now = DateTime.UtcNow;
        var changed = await dbContext.SupplierOrders
            .Where(item => item.Id == supplierOrderId && item.SupplierId == supplierId && item.Status == supplierOrder.Status)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, request.Status)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);
        if (changed != 1) throw new ConflictException("Supplier order changed while you were updating it. Reload and try again.");

        if (request.Status == "rejected")
        {
            if (await dbContext.Payments.AnyAsync(item => item.OrderId == supplierOrder.OrderId
                    && item.Status == "successful", cancellationToken))
                throw new ConflictException("This order has a successful payment and requires the refund workflow before rejection.");
            foreach (var group in supplierOrder.Items.GroupBy(line => line.SupplierOfferId))
            {
                var quantity = group.Sum(line => line.Quantity);
                var balance = await dbContext.InventoryBalances.AsNoTracking().SingleOrDefaultAsync(item =>
                    item.SupplierLocationId == supplierOrder.SupplierLocationId && item.SupplierOfferId == group.Key,
                    cancellationToken);
                if (balance is null) throw new ConflictException("Reserved stock balance is missing; contact support before retrying.");
                var released = await dbContext.InventoryBalances.Where(item => item.Id == balance.Id && item.QuantityReserved >= quantity)
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.QuantityReserved, item => item.QuantityReserved - quantity)
                        .SetProperty(item => item.UpdatedAt, now), cancellationToken);
                if (released != 1) throw new ConflictException("The order reservation is no longer available for release.");
                dbContext.InventoryMovements.Add(new InventoryMovement
                {
                    InventoryBalanceId = balance.Id, MovementType = "release", QuantityDelta = quantity,
                    ReferenceType = "supplier_order", ReferenceId = supplierOrderId,
                    Notes = "Supplier rejected order", CreatedAt = now
                });
            }
            foreach (var deliveryId in supplierOrder.DeliveryIds)
            {
                var deliveryUpdated = await dbContext.Deliveries.Where(item => item.Id == deliveryId && item.Status == "pending")
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, "cancelled"), cancellationToken);
                if (deliveryUpdated == 1)
                    dbContext.DeliveryStatusHistories.Add(new DeliveryStatusHistory
                    {
                        DeliveryId = deliveryId, Status = "cancelled", Notes = "Supplier rejected the order.",
                        RecordedByUserId = userId, CreatedAt = now
                    });
            }
            var hasOtherActiveSupplierOrders = await dbContext.SupplierOrders.AnyAsync(item => item.OrderId == supplierOrder.OrderId
                && item.Id != supplierOrderId && item.Status != "rejected" && item.Status != "cancelled", cancellationToken);
            var remainingSubtotal = hasOtherActiveSupplierOrders
                ? await dbContext.SupplierOrders.Where(item => item.OrderId == supplierOrder.OrderId
                    && item.Status != "rejected" && item.Status != "cancelled")
                    .SumAsync(item => (decimal?)item.Subtotal, cancellationToken) ?? 0m
                : 0m;
            var order = await dbContext.Orders.AsNoTracking().Where(item => item.Id == supplierOrder.OrderId)
                .Select(item => new { item.DeliveryFee, item.DiscountAmount }).SingleAsync(cancellationToken);
            var remainingDeliveryFee = hasOtherActiveSupplierOrders ? order.DeliveryFee : 0m;
            var remainingDiscount = hasOtherActiveSupplierOrders
                ? Math.Min(order.DiscountAmount, remainingSubtotal + remainingDeliveryFee)
                : 0m;
            var remainingTotal = decimal.Round(remainingSubtotal + remainingDeliveryFee - remainingDiscount, 2);
            await dbContext.Orders.Where(item => item.Id == supplierOrder.OrderId)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, hasOtherActiveSupplierOrders ? "partially_fulfilled" : "failed")
                    .SetProperty(item => item.Subtotal, remainingSubtotal)
                    .SetProperty(item => item.DeliveryFee, remainingDeliveryFee)
                    .SetProperty(item => item.DiscountAmount, remainingDiscount)
                    .SetProperty(item => item.TotalAmount, remainingTotal)
                    .SetProperty(item => item.UpdatedAt, now), cancellationToken);
            await dbContext.Payments.Where(item => item.OrderId == supplierOrder.OrderId
                    && item.Method == "cash" && item.Status == "pending")
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.Amount, item => remainingTotal > 0 ? remainingTotal : item.Amount)
                    .SetProperty(item => item.Status, remainingTotal > 0 ? "pending" : "cancelled"), cancellationToken);
        }
        else
        {
            var orderStatus = request.Status == "accepted" ? "confirmed" : "processing";
            await dbContext.Orders.Where(item => item.Id == supplierOrder.OrderId
                    && (item.Status == "pending" || item.Status == "confirmed" || item.Status == "processing"))
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.Status, orderStatus)
                    .SetProperty(item => item.UpdatedAt, now), cancellationToken);
        }

        dbContext.SupplierOrderStatusHistories.Add(new SupplierOrderStatusHistory
        {
            SupplierOrderId = supplierOrderId,
            Status = request.Status,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RecordedByUserId = userId,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record SupplierOrderStatusRequest([property: Required] string Status,
    [property: StringLength(1000)] string? Notes);
public sealed record SupplierOrderLineResponse(Guid Id, string ProductName, decimal Quantity,
    string UnitCode, decimal UnitPrice, decimal LineTotal);
public sealed record SupplierOrderEvent(string Status, string? Notes, DateTime CreatedAt);
public sealed record SupplierOrderResponse(Guid Id, string OrderNumber, string Status, Guid LocationId,
    string LocationName, decimal Subtotal, string Currency, string RecipientName, string RecipientPhone,
    string DeliveryAddress, string DeliveryCity, string? CustomerNotes, DateTime CreatedAt,
    IReadOnlyList<SupplierOrderLineResponse> Items, IReadOnlyList<SupplierOrderEvent> StatusHistory);
