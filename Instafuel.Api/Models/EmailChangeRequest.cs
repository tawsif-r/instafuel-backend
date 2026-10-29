namespace Instafuel.Api.Models;

public sealed class EmailChangeRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string NewEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    public User User { get; set; } = null!;
}
