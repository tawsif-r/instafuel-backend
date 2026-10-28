namespace Instafuel.Api.Modules.Identity.DTOs;

public sealed record RegisterRequest(
    string FullName,
    string? Email,
    string Password,
    string? Phone,
    string? DeviceInfo = null);

public sealed record RegisterRiderRequest(
    string FullName,
    string? Email,
    string Password,
    string? Phone,
    string VehicleType,
    string VehicleRegistrationNumber,
    string? DeviceInfo = null);

public sealed record LoginRequest(string? Email, string Password, string? Phone = null, string? DeviceInfo = null);

public sealed record RefreshRequest(string RefreshToken, string? DeviceInfo = null);
