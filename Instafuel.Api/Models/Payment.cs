using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "BDT";
    public string Method { get; set; } = string.Empty;   // cash | card | mobile_banking | bank_transfer
    public string Status { get; set; } = "pending";      // pending | successful | failed | cancelled
    public string? ProviderReference { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
