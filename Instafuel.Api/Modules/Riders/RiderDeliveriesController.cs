using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Instafuel.Api.Authorization;
using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Riders;

[Authorize(Policy = AuthorizationPolicies.Rider)]
[Route("api/v1/rider")]
public sealed class RiderDeliveriesController(
    PgDbContext dbContext,
    IHubContext<DeliveryTrackingHub> trackingHub) : BaseApiController
{
    [HttpGet("jobs/available")]
    public async Task<ActionResult<IReadOnlyList<RiderJobResponse>>> GetAvailableJobs(CancellationToken cancellationToken)
    {
        var riderId = GetRiderId();
        await RequireOnlineRider(riderId, cancellationToken);
        var jobs = await dbContext.Deliveries.AsNoTracking()
            .Where(delivery => delivery.DeliveryAgentId == null && delivery.Status == "pending"
                && delivery.SupplierOrder.Status == "ready"
                && delivery.SupplierOrder.SupplierLocation.IsActive
                && dbContext.RiderProfiles.Any(profile => profile.UserId == riderId
                    && profile.PreferredAreas.Any(area => area.City.ToLower() == delivery.SupplierOrder.SupplierLocation.City.ToLower())))
            .OrderBy(delivery => delivery.CreatedAt)
            .Select(delivery => new RiderJobResponse(delivery.Id, delivery.SupplierOrder.Order.OrderNumber,
                delivery.SupplierOrder.Supplier.DisplayName, delivery.SupplierOrder.SupplierLocation.Name,
                delivery.SupplierOrder.SupplierLocation.AddressLine, delivery.SupplierOrder.SupplierLocation.City,
                delivery.SupplierOrder.Order.RecipientName, delivery.SupplierOrder.Order.RecipientPhone,
                delivery.SupplierOrder.Order.DeliveryAddress, delivery.SupplierOrder.Order.DeliveryCity,
                delivery.SupplierOrder.Order.DeliveryLatitude, delivery.SupplierOrder.Order.DeliveryLongitude,
                delivery.SupplierOrder.Subtotal,
                delivery.SupplierOrder.Items.Select(item => new RiderJobItem(item.ProductName, item.Quantity, item.UnitCode)).ToList()))
            .ToListAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("deliveries")]
    public async Task<ActionResult<IReadOnlyList<RiderAssignedDeliveryResponse>>> GetMyDeliveries(CancellationToken cancellationToken)
    {
        var riderId = GetRiderId();
        var deliveries = await dbContext.Deliveries.AsNoTracking()
            .Where(delivery => delivery.DeliveryAgentId == riderId
                && delivery.Status != "delivered" && delivery.Status != "failed" && delivery.Status != "cancelled")
            .OrderBy(delivery => delivery.AssignedAt)
            .Select(delivery => new RiderAssignedDeliveryResponse(delivery.Id,
                delivery.SupplierOrder.Order.OrderNumber, delivery.Status,
                delivery.SupplierOrder.Order.RecipientName, delivery.SupplierOrder.Order.RecipientPhone,
                delivery.SupplierOrder.Order.DeliveryAddress, delivery.SupplierOrder.Order.DeliveryCity,
                delivery.SupplierOrder.Order.DeliveryLatitude, delivery.SupplierOrder.Order.DeliveryLongitude,
                delivery.SupplierOrder.Subtotal, delivery.AssignedAt, delivery.PickedUpAt))
            .ToListAsync(cancellationToken);
        return Ok(deliveries);
    }

    [HttpPost("jobs/{deliveryId:guid}/accept")]
    public async Task<ActionResult<RiderAssignedDeliveryResponse>> AcceptJob(Guid deliveryId, CancellationToken cancellationToken)
    {
        var riderId = GetRiderId();
        await RequireOnlineRider(riderId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var assigned = await dbContext.Deliveries
            .Where(delivery => delivery.Id == deliveryId && delivery.Status == "pending"
                && delivery.DeliveryAgentId == null && delivery.SupplierOrder.Status == "ready"
                && delivery.SupplierOrder.SupplierLocation.IsActive
                && dbContext.RiderProfiles.Any(profile => profile.UserId == riderId
                    && profile.VerificationStatus == "verified" && profile.IsAvailable
                    && profile.PreferredAreas.Any(area => area.City.ToLower() == delivery.SupplierOrder.SupplierLocation.City.ToLower())))
            .ExecuteUpdateAsync(update => update
                .SetProperty(delivery => delivery.DeliveryAgentId, riderId)
                .SetProperty(delivery => delivery.Status, "assigned")
                .SetProperty(delivery => delivery.AssignedAt, now), cancellationToken);
        if (assigned != 1)
            throw new ConflictException("This delivery is no longer available or is outside your preferred areas.");

        dbContext.DeliveryStatusHistories.Add(new DeliveryStatusHistory
        {
            DeliveryId = deliveryId, Status = "assigned", RecordedByUserId = riderId,
            Notes = "Accepted by rider", CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var result = await GetAssignedResponse(deliveryId, riderId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("deliveries/{deliveryId:guid}/decline")]
    public async Task<IActionResult> DeclineJob(Guid deliveryId, CancellationToken cancellationToken)
    {
        var riderId = GetRiderId();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = await dbContext.Deliveries
            .Where(delivery => delivery.Id == deliveryId && delivery.DeliveryAgentId == riderId && delivery.Status == "assigned")
            .ExecuteUpdateAsync(update => update
                .SetProperty(delivery => delivery.DeliveryAgentId, (Guid?)null)
                .SetProperty(delivery => delivery.Status, "pending")
                .SetProperty(delivery => delivery.AssignedAt, (DateTime?)null), cancellationToken);
        if (updated != 1) throw new ConflictException("Only an assigned delivery can be declined.");
        dbContext.DeliveryStatusHistories.Add(new DeliveryStatusHistory
        {
            DeliveryId = deliveryId, Status = "pending", RecordedByUserId = riderId,
            Notes = "Rider declined assignment", CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("deliveries/{deliveryId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid deliveryId, UpdateRiderDeliveryStatusRequest request, CancellationToken cancellationToken)
    {
        var riderId = GetRiderId();
        await RequireVerifiedRider(riderId, cancellationToken);
        if (request.Notes?.Length > 1000 || request.ProofOfDeliveryReference?.Length > 500
            || request.SafetyChecklist?.Notes?.Length > 1000)
            throw new AppValidationException("Notes or proof reference exceeds the maximum length.");
        if (request.Status is not ("picked_up" or "in_transit" or "delivered" or "failed"))
            throw new AppValidationException(nameof(request.Status), "Unsupported rider delivery status.");
        if (request.Status == "failed" && string.IsNullOrWhiteSpace(request.Notes))
            throw new AppValidationException(nameof(request.Notes), "A failure reason is required for failed deliveries.");

        var delivery = await dbContext.Deliveries.AsNoTracking()
            .Include(item => item.SupplierOrder).ThenInclude(item => item.Order)
            .SingleOrDefaultAsync(item => item.Id == deliveryId && item.DeliveryAgentId == riderId, cancellationToken);
        if (delivery is null) return NotFound();
        if (!DeliveryWorkflow.CanTransition(delivery.Status, request.Status))
            throw new ConflictException($"Delivery cannot move from '{delivery.Status}' to '{request.Status}'.");

        var now = DateTime.UtcNow;
        if (request.Status == "picked_up" && (request.SafetyChecklist is null || !request.SafetyChecklist.IsComplete))
            throw new AppValidationException(nameof(request.SafetyChecklist), "Complete every fuel safety checklist item before pickup.");
        if (request.Status == "delivered" && string.IsNullOrWhiteSpace(request.ProofOfDeliveryReference))
            throw new AppValidationException(nameof(request.ProofOfDeliveryReference), "Proof of delivery is required.");
        if (request.CashCollectedAmount is < 0 or > 999999999999.99m)
            throw new AppValidationException(nameof(request.CashCollectedAmount), "Cash collected amount is outside the allowed range.");
        if (request.CashCollectedAmount.HasValue && request.CashCollectedAmount.Value != decimal.Round(request.CashCollectedAmount.Value, 2))
            throw new AppValidationException(nameof(request.CashCollectedAmount), "Cash amount supports at most 2 decimal places.");
        if (request.Status != "delivered" && request.CashCollectedAmount.GetValueOrDefault() != 0)
            throw new AppValidationException(nameof(request.CashCollectedAmount), "Cash can only be recorded when delivery is completed.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        Payment? cashPayment = null;
        if (request.Status == "delivered")
        {
            var pendingPayment = await dbContext.Payments.AsNoTracking().SingleOrDefaultAsync(payment =>
                payment.OrderId == delivery.SupplierOrder.OrderId && payment.Method == "cash" && payment.Status == "pending", cancellationToken);
            if (pendingPayment is not null)
            {
                cashPayment = await dbContext.Payments
                    .FromSqlInterpolated($"SELECT * FROM payments WHERE id = {pendingPayment.Id} FOR UPDATE")
                    .SingleAsync(cancellationToken);
                if (cashPayment.Status != "pending")
                    throw new ConflictException("The cash-on-delivery payment changed while updating the delivery.");
            }
            var successfulPaymentExists = await dbContext.Payments.AnyAsync(payment =>
                payment.OrderId == delivery.SupplierOrder.OrderId && payment.Status == "successful", cancellationToken);
            if (cashPayment is null && !successfulPaymentExists)
                throw new ConflictException("An active COD payment or successful online payment is required before completing delivery.");
            if (successfulPaymentExists && request.CashCollectedAmount.GetValueOrDefault() > 0)
                throw new ConflictException("Payment has already been completed online; do not collect cash.");
            if (successfulPaymentExists && cashPayment is not null)
                cashPayment.Status = "cancelled";

            if (cashPayment is not null)
            {
                var previouslyCollected = await dbContext.Deliveries.AsNoTracking()
                    .Where(item => item.Id != deliveryId && item.SupplierOrder.OrderId == delivery.SupplierOrder.OrderId)
                    .SumAsync(item => (decimal?)item.CashCollectedAmount, cancellationToken) ?? 0m;
                var totalCollected = previouslyCollected + request.CashCollectedAmount.GetValueOrDefault();
                if (totalCollected > cashPayment.Amount)
                    throw new ConflictException("Cash collected would exceed the order amount due.");
                var allOtherDeliveriesDone = await dbContext.Deliveries.AsNoTracking()
                    .Where(item => item.Id != deliveryId && item.SupplierOrder.OrderId == delivery.SupplierOrder.OrderId)
                    .AllAsync(item => item.Status == "delivered", cancellationToken);
                if (allOtherDeliveriesDone && totalCollected != cashPayment.Amount)
                    throw new ConflictException("Collect the exact remaining COD amount before completing the final delivery.");
                if (totalCollected == cashPayment.Amount)
                {
                    cashPayment.Status = "successful";
                    cashPayment.PaidAt = now;
                }
            }
        }

        var changed = await dbContext.Deliveries
            .Where(item => item.Id == deliveryId && item.DeliveryAgentId == riderId && item.Status == delivery.Status)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.Status, request.Status)
                .SetProperty(item => item.PickedUpAt, item => request.Status == "picked_up" ? now : item.PickedUpAt)
                .SetProperty(item => item.DeliveredAt, item => request.Status == "delivered" ? now : item.DeliveredAt)
                .SetProperty(item => item.ProofOfDeliveryReference,
                    item => request.Status == "delivered" ? request.ProofOfDeliveryReference : item.ProofOfDeliveryReference)
                .SetProperty(item => item.SafetyChecklistCompletedAt,
                    item => request.Status == "picked_up" ? now : item.SafetyChecklistCompletedAt)
                .SetProperty(item => item.SafetyChecklistNotes,
                    item => request.Status == "picked_up" ? request.SafetyChecklist!.Notes : item.SafetyChecklistNotes)
                .SetProperty(item => item.SafetyChecklistJson,
                    item => request.Status == "picked_up" ? JsonSerializer.Serialize(request.SafetyChecklist) : item.SafetyChecklistJson)
                .SetProperty(item => item.CashCollectedAmount,
                    item => request.Status == "delivered" ? request.CashCollectedAmount.GetValueOrDefault() : item.CashCollectedAmount), cancellationToken);
        if (changed != 1) throw new ConflictException("Delivery changed while you were updating it. Reload and try again.");

        dbContext.DeliveryStatusHistories.Add(new DeliveryStatusHistory
        {
            DeliveryId = deliveryId, Status = request.Status, Notes = request.Notes,
            RecordedByUserId = riderId, CreatedAt = now
        });
        if (cashPayment is not null) dbContext.Entry(cashPayment).State = EntityState.Modified;
        var supplierOrder = await dbContext.SupplierOrders.SingleAsync(item => item.Id == delivery.SupplierOrderId, cancellationToken);
        var previousSupplierOrderStatus = supplierOrder.Status;
        if (request.Status is "picked_up" or "in_transit") supplierOrder.Status = "dispatched";
        if (request.Status == "delivered") supplierOrder.Status = "delivered";
        supplierOrder.UpdatedAt = now;
        if (supplierOrder.Status != previousSupplierOrderStatus)
            dbContext.SupplierOrderStatusHistories.Add(new SupplierOrderStatusHistory
            {
                SupplierOrderId = supplierOrder.Id,
                Status = supplierOrder.Status,
                Notes = request.Status == "delivered" ? "Rider completed delivery." : "Rider picked up the supplier order.",
                RecordedByUserId = riderId,
                CreatedAt = now
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.Status == "delivered")
        {
            var allSupplierOrdersDelivered = await dbContext.SupplierOrders
                .Where(item => item.OrderId == delivery.SupplierOrder.OrderId)
                .AllAsync(item => item.Id == supplierOrder.Id || item.Status == "delivered", cancellationToken);
            if (allSupplierOrdersDelivered)
                await dbContext.Orders.Where(item => item.Id == delivery.SupplierOrder.OrderId)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(item => item.Status, "completed")
                        .SetProperty(item => item.UpdatedAt, now), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("deliveries/{deliveryId:guid}/location")]
    public async Task<IActionResult> UpdateLocation(
        Guid deliveryId, RiderLocationRequest request, CancellationToken cancellationToken)
    {
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            throw new AppValidationException("Coordinates are outside valid latitude or longitude ranges.");
        var riderId = GetRiderId();
        await RequireVerifiedRider(riderId, cancellationToken);
        var delivery = await dbContext.Deliveries.AsNoTracking()
            .Where(item => item.Id == deliveryId && item.DeliveryAgentId == riderId
                && (item.Status == "assigned" || item.Status == "picked_up" || item.Status == "in_transit"))
            .Select(item => new { item.Id, item.SupplierOrder.OrderId })
            .SingleOrDefaultAsync(cancellationToken);
        if (delivery is null) return NotFound();

        var now = DateTime.UtcNow;
        var updated = await dbContext.Deliveries
            .Where(item => item.Id == deliveryId && item.DeliveryAgentId == riderId
                && (item.Status == "assigned" || item.Status == "picked_up" || item.Status == "in_transit"))
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.RiderLatitude, request.Latitude)
                .SetProperty(item => item.RiderLongitude, request.Longitude)
                .SetProperty(item => item.LocationUpdatedAt, now), cancellationToken);
        if (updated != 1) throw new ConflictException("Delivery is no longer active.");

        await trackingHub.Clients.Group(DeliveryTrackingHub.OrderGroup(delivery.OrderId))
            .SendAsync("riderLocationUpdated", new RiderLocationUpdate(deliveryId, request.Latitude, request.Longitude, now), cancellationToken);
        return NoContent();
    }

    private Guid GetRiderId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedException();
    }

    private async Task<RiderProfile> RequireOnlineRider(Guid riderId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.RiderProfiles.SingleOrDefaultAsync(item => item.UserId == riderId, cancellationToken)
            ?? throw new NotFoundException("Rider profile was not found.");
        if (profile.VerificationStatus != "verified") throw new ForbiddenException("Rider verification is required.");
        if (!profile.IsAvailable) throw new ConflictException("Go online to view and accept rider jobs.");
        return profile;
    }

    private async Task RequireVerifiedRider(Guid riderId, CancellationToken cancellationToken)
    {
        var isVerified = await dbContext.RiderProfiles.AnyAsync(profile => profile.UserId == riderId
            && profile.VerificationStatus == "verified", cancellationToken);
        if (!isVerified) throw new ForbiddenException("An active rider verification is required.");
    }

    private async Task<RiderAssignedDeliveryResponse> GetAssignedResponse(Guid deliveryId, Guid riderId, CancellationToken cancellationToken) =>
        await dbContext.Deliveries.AsNoTracking()
            .Where(item => item.Id == deliveryId && item.DeliveryAgentId == riderId)
            .Select(item => new RiderAssignedDeliveryResponse(item.Id, item.SupplierOrder.Order.OrderNumber,
                item.Status, item.SupplierOrder.Order.RecipientName, item.SupplierOrder.Order.RecipientPhone,
                item.SupplierOrder.Order.DeliveryAddress, item.SupplierOrder.Order.DeliveryCity,
                item.SupplierOrder.Order.DeliveryLatitude, item.SupplierOrder.Order.DeliveryLongitude,
                item.SupplierOrder.Subtotal, item.AssignedAt, item.PickedUpAt))
            .SingleAsync(cancellationToken);
}

public sealed record PickupSafetyChecklist(bool ContainerSecured, bool EngineOff, bool NoSmokingArea,
    bool NoLeaks, bool ProtectiveEquipmentUsed, string? Notes)
{
    public bool IsComplete => ContainerSecured && EngineOff && NoSmokingArea && NoLeaks && ProtectiveEquipmentUsed;
}
public sealed record UpdateRiderDeliveryStatusRequest(string Status, string? Notes,
    string? ProofOfDeliveryReference, decimal? CashCollectedAmount, PickupSafetyChecklist? SafetyChecklist);
public sealed record RiderLocationRequest(decimal Latitude, decimal Longitude);
public sealed record RiderLocationUpdate(Guid DeliveryId, decimal Latitude, decimal Longitude, DateTime UpdatedAt);
public sealed record RiderJobItem(string ProductName, decimal Quantity, string UnitCode);
public sealed record RiderJobResponse(Guid DeliveryId, string OrderNumber, string SupplierName,
    string SupplierLocationName, string SupplierAddress, string SupplierCity, string RecipientName,
    string RecipientPhone, string DeliveryAddress, string DeliveryCity, decimal? DeliveryLatitude,
    decimal? DeliveryLongitude, decimal SupplierOrderSubtotal, IReadOnlyList<RiderJobItem> Items);
public sealed record RiderAssignedDeliveryResponse(Guid DeliveryId, string OrderNumber, string Status,
    string RecipientName, string RecipientPhone, string DeliveryAddress, string DeliveryCity,
    decimal? DeliveryLatitude, decimal? DeliveryLongitude, decimal SupplierOrderSubtotal,
    DateTime? AssignedAt, DateTime? PickedUpAt);
