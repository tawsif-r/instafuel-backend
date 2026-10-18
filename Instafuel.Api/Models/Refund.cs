using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class Refund
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    public string Status { get; set; } = "pending"; // pending | successful | failed | cancelled
    public string? ProviderReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Payment Payment { get; set; } = null!;
}
