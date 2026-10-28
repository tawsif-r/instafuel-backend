using Instafuel.Api.Controllers;
using Instafuel.Api.Exceptions;
using Instafuel.Api.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Modules.Riders;

[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin/riders")]
public sealed class AdminRidersController(PgDbContext dbContext) : BaseApiController
{
    [HttpGet("failed-deliveries")]
    public async Task<ActionResult<IReadOnlyList<FailedDeliveryRecord>>> GetFailedDeliveries(CancellationToken cancellationToken)
    {
        var failed = await dbContext.Deliveries.AsNoTracking()
            .Where(delivery => delivery.Status == "failed")
            .OrderBy(delivery => delivery.CreatedAt)
            .Select(delivery => new FailedDeliveryRecord(delivery.Id, delivery.SupplierOrder.Order.OrderNumber,
                delivery.SupplierOrder.Supplier.DisplayName, delivery.DeliveryAgentId,
                delivery.SupplierOrder.Status, delivery.SupplierOrder.Order.DeliveryCity, delivery.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(failed);
    }

    [HttpPost("failed-deliveries/{deliveryId:guid}/retry")]
    public async Task<IActionResult> RetryFailedDelivery(Guid deliveryId, CancellationToken cancellationToken)
    {
        var actor = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) ? actorId : (Guid?)null;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var delivery = await dbContext.Deliveries
            .Include(item => item.SupplierOrder)
            .SingleOrDefaultAsync(item => item.Id == deliveryId && item.Status == "failed", cancellationToken);
        if (delivery is null) return NotFound();
        if (delivery.SupplierOrder.Status is "cancelled" or "rejected")
            throw new ConflictException("A cancelled or rejected supplier order cannot be retried.");

        var now = DateTime.UtcNow;
        delivery.Status = "pending";
        delivery.DeliveryAgentId = null;
        delivery.AssignedAt = null;
        delivery.PickedUpAt = null;
        delivery.DeliveredAt = null;
        delivery.ProofOfDeliveryReference = null;
        delivery.SafetyChecklistCompletedAt = null;
        delivery.SafetyChecklistNotes = null;
        delivery.SafetyChecklistJson = null;
        delivery.CashCollectedAmount = 0m;
        delivery.RiderLatitude = null;
        delivery.RiderLongitude = null;
        delivery.LocationUpdatedAt = null;
        delivery.SupplierOrder.Status = "ready";
        delivery.SupplierOrder.UpdatedAt = now;
        dbContext.SupplierOrderStatusHistories.Add(new SupplierOrderStatusHistory
        {
            SupplierOrderId = delivery.SupplierOrderId,
            Status = "ready",
            Notes = "Admin reopened failed delivery for reassignment.",
            RecordedByUserId = actor,
            CreatedAt = now
        });
        dbContext.DeliveryStatusHistories.Add(new DeliveryStatusHistory
        {
            DeliveryId = delivery.Id,
            Status = "pending",
            Notes = "Admin reopened failed delivery for reassignment.",
            RecordedByUserId = actor,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<RiderVerificationRecord>>> GetPending(CancellationToken cancellationToken)
    {
        var pending = await dbContext.RiderProfiles.AsNoTracking()
            .Where(profile => profile.VerificationStatus == "pending")
            .OrderBy(profile => profile.CreatedAt)
            .Select(profile => new RiderVerificationRecord(profile.UserId, profile.User.FullName,
                profile.User.Phone, profile.VehicleType, profile.VehicleRegistrationNumber,
                profile.VerificationStatus, profile.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(pending);
    }

    [HttpPut("{riderUserId:guid}/verification")]
    public async Task<IActionResult> UpdateVerification(
        Guid riderUserId, RiderVerificationRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is not ("verified" or "rejected" or "suspended"))
            throw new AppValidationException(nameof(request.Status), "Status must be verified, rejected, or suspended.");
        var profile = await dbContext.RiderProfiles.SingleOrDefaultAsync(
            item => item.UserId == riderUserId, cancellationToken);
        if (profile is null) return NotFound();
        profile.VerificationStatus = request.Status;
        profile.IsAvailable = false;
        profile.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record RiderVerificationRequest(string Status);
public sealed record FailedDeliveryRecord(Guid DeliveryId, string OrderNumber, string SupplierName,
    Guid? PreviousRiderId, string SupplierOrderStatus, string DeliveryCity, DateTime CreatedAt);
public sealed record RiderVerificationRecord(Guid UserId, string FullName, string? Phone,
    string? VehicleType, string? VehicleRegistrationNumber, string VerificationStatus, DateTime SubmittedAt);
