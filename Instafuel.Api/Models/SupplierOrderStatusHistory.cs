namespace Instafuel.Api.Models;

public class SupplierOrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid SupplierOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupplierOrder SupplierOrder { get; set; } = null!;
    public User? RecordedByUser { get; set; }
}
