using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

public record CreateDeliveryRequest(Guid SupplierOrderId);

public record AssignDeliveryRequest(Guid DriverId, Guid VehicleId);

public record UpdateDeliveryStatusRequest(
    [Required, AllowedValues("dispatched", "in_transit", "delivered", "failed", "cancelled")] string Status,
    [Range(0, 1_000_000)] decimal? DeliveredQuantity, // required when Status = delivered
    [MaxLength(500)] string? FailureReason,           // required when Status = failed
    string? Notes,
    Guid? RecordedByUserId);
