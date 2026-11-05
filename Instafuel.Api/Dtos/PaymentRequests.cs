using System.ComponentModel.DataAnnotations;

namespace Instafuel.Api.Dtos;

public record CreatePaymentRequest(
    Guid OrderId,
    [Range(0.01, 100_000_000)] decimal Amount,
    [Required, AllowedValues("cash", "card", "mobile_banking", "bank_transfer")] string Method,
    [Required, MaxLength(100)] string IdempotencyKey, // client generates it; retrying with the same key won't double-charge
    string? ProviderReference);

public record UpdatePaymentStatusRequest(
    [Required, AllowedValues("pending", "successful", "failed", "cancelled")] string Status,
    string? ProviderReference);
