namespace Instafuel.Api.Models;

public sealed class OtpCode
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int Attempts { get; set; }
    public User? User { get; set; }
}
