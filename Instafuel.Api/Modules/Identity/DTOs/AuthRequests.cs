namespace Instafuel.Api.Modules.Identity.DTOs;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);
