using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

// Password/auth is not handled yet, so no password field here.
public record CreateUserRequest(
    [Required, MaxLength(150)] string FullName,
    [EmailAddress, MaxLength(255)] string? Email,
    [MaxLength(20)] string? Phone);

public record UpdateUserRequest(
    [Required, MaxLength(150)] string FullName,
    [EmailAddress, MaxLength(255)] string? Email,
    [MaxLength(20)] string? Phone,
    bool IsActive);
