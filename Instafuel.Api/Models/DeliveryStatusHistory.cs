namespace Instafuel.Api.Models;

public class DeliveryStatusHistory
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Delivery Delivery { get; set; } = null!;
    public User? RecordedByUser { get; set; }
}
