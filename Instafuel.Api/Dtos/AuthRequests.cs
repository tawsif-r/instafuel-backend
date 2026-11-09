using System.ComponentModel.DataAnnotations;
using Instafuel.Api.Models;

namespace Instafuel.Api.Dtos;

// Customer self sign-up. Drivers/staff/admins are created by an admin later.
public record RegisterRequest(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MaxLength(20)] string Phone,
    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters."), MaxLength(100)] string Password,
    [AllowedValues("individual", "corporate")] string AccountType = "individual",
    [MaxLength(150)] string? CompanyName = null);

// Identifier is an email or a phone number.
public record LoginRequest(
    [Required] string Identifier,
    [Required] string Password);

// Returned by register and login. The app sends Token as "Authorization: Bearer <token>".
public record AuthResponse(string Token, DateTime ExpiresAt, User User);
