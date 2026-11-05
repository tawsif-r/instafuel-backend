using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

// Used for both create (POST) and update (PUT). Documents is the full list:
// a kind left out is removed from the vehicle.
public record SaveCustomerVehicleRequest(
    [Required, MaxLength(60)] string Nickname,
    [Required, AllowedValues("car", "motorcycle", "microbus", "pickup", "truck", "generator")] string VehicleType,
    [Required, MaxLength(30)] string RegistrationNumber,
    [Required, AllowedValues("octane", "petrol", "diesel")] string FuelType,
    List<VehicleDocumentRequest>? Documents);

public record VehicleDocumentRequest(
    [Required, AllowedValues("registration_certificate", "fitness_certificate", "number_plate_photo")] string Kind,
    [Required, MaxLength(255)] string FileName);

// Admin decision on a vehicle's documents.
public record VerifyCustomerVehicleRequest(
    [Required, AllowedValues("verified", "rejected")] string Status,
    [MaxLength(300)] string? RejectionReason);
