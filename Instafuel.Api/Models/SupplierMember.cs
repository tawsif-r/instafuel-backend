using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

[PrimaryKey(nameof(SupplierId), nameof(UserId))]
public class SupplierMember
{
    public Guid SupplierId { get; set; }
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = "staff"; // owner | manager | staff
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Supplier Supplier { get; set; } = null!;
    public User User { get; set; } = null!;
}
