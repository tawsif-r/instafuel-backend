using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class SupplierOrder
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SupplierLocationId { get; set; }
    public string Status { get; set; } = "pending"; // pending | accepted | rejected | preparing | ready | dispatched | delivered | cancelled

    [Precision(14, 2)]
    public decimal Subtotal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public SupplierLocation SupplierLocation { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
