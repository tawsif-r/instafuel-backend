using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class SupplierOffer
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public Guid ProductId { get; set; }
    public string? SellerSku { get; set; } // SKU -> Stock keeping unit, kotogula ase serfexel??

    [Precision(14, 2)] // total number of digit 14 and numbers after decimal 2.
    public decimal UnitPrice { get; set; }

    public string Currency { get; set; } = "BDT";
    public string Status { get; set; } = "active"; // active | paused | discontinued
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Supplier Supplier { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>(); // one to many relationship
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
