namespace Instafuel.Api.Modules.Identity.DTOs;

public sealed record AuthResponse(
    Guid UserId,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record CurrentUserResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    IReadOnlyList<string> Roles);
