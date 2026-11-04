using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class Delivery
{
    public Guid Id { get; set; }
    public Guid SupplierOrderId { get; set; }
    public Guid? DriverId { get; set; }  // set when the delivery is assigned
    public Guid? VehicleId { get; set; } // set when the delivery is assigned
    public string Status { get; set; } = "pending"; // pending | assigned | dispatched | in_transit | delivered | failed | cancelled
    public DateTime? AssignedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    [Precision(14, 3)]
    public decimal? DeliveredQuantity { get; set; } // actual quantity handed over, required when delivered

    public string? FailureReason { get; set; }      // required when failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupplierOrder SupplierOrder { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public ICollection<DeliveryStatusHistory> StatusHistory { get; set; } = new List<DeliveryStatusHistory>();
}
