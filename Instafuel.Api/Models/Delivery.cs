using Microsoft.EntityFrameworkCore;

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
    public string? ProofOfDeliveryReference { get; set; }
    public DateTime? SafetyChecklistCompletedAt { get; set; }
    public string? SafetyChecklistNotes { get; set; }
    public string? SafetyChecklistJson { get; set; }
    [Precision(14, 2)]
    public decimal CashCollectedAmount { get; set; }
    [Precision(9, 6)]
    public decimal? RiderLatitude { get; set; }
    [Precision(9, 6)]
    public decimal? RiderLongitude { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupplierOrder SupplierOrder { get; set; } = null!;
    public User? DeliveryAgent { get; set; }
    public ICollection<DeliveryStatusHistory> StatusHistory { get; set; } = new List<DeliveryStatusHistory>();
}
