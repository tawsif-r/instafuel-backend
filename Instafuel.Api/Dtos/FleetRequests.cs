using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

public record CreateDriverRequest(
    Guid UserId,
    [Required, MaxLength(50)] string LicenseNumber,
    DateOnly LicenseExpiry,
    bool DangerousGoodsCertified);

public record UpdateDriverRequest(
    [Required, MaxLength(50)] string LicenseNumber,
    DateOnly LicenseExpiry,
    bool DangerousGoodsCertified,
    [Required, AllowedValues("pending", "verified", "rejected")] string VerificationStatus,
    [Required, AllowedValues("active", "on_leave", "terminated")] string EmploymentStatus);

public record UpdateAvailabilityRequest(bool IsAvailable);

// Used for both create (POST) and update (PUT).
public record SaveVehicleRequest(
    [Required, MaxLength(30)] string RegistrationNumber,
    [Required, AllowedValues("tanker", "bowser", "pickup")] string VehicleType,
    [Range(1, 100_000)] decimal CapacityLitres,
    [Range(1, 20)] int CompartmentCount,
    DateOnly? FitnessCertificateExpiry,
    DateOnly? LastInspectionDate);

public record UpdateVehicleStatusRequest(
    [Required, AllowedValues("active", "under_maintenance", "retired")] string Status);
