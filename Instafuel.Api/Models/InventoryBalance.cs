using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class InventoryBalance
{
    public Guid Id { get; set; }
    public Guid SupplierLocationId { get; set; }
    public Guid SupplierOfferId { get; set; }

    public uint Version { get; set; }

    [Precision(14, 3)]
    public decimal QuantityOnHand { get; set; }

    [Precision(14, 3)]
    public decimal QuantityReserved { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SupplierLocation SupplierLocation { get; set; } = null!;
    public SupplierOffer SupplierOffer { get; set; } = null!;
    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
}
