using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class InventoryMovement
{
    public Guid Id { get; set; }
    public Guid InventoryBalanceId { get; set; }
    public string MovementType { get; set; } = string.Empty; // receipt | reservation | release | sale | adjustment | return

    [Precision(14, 3)]
    public decimal QuantityDelta { get; set; }

    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public InventoryBalance InventoryBalance { get; set; } = null!;
}
