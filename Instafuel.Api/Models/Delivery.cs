namespace Instafuel.Api.Models;

public class Delivery
{
    public Guid Id { get; set; }
    public Guid SupplierOrderId { get; set; }
    public Guid? DeliveryAgentId { get; set; }
    public string Status { get; set; } = "pending"; // pending | assigned | picked_up | in_transit | delivered | failed | cancelled
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupplierOrder SupplierOrder { get; set; } = null!;
    public User? DeliveryAgent { get; set; }
    public ICollection<DeliveryStatusHistory> StatusHistory { get; set; } = new List<DeliveryStatusHistory>();
}
