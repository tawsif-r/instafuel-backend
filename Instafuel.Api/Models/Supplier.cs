namespace Instafuel.Api.Models;

public class Supplier
{
    public Guid Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending | active | suspended | rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierMember> Members { get; set; } = new List<SupplierMember>();
    public ICollection<SupplierLocation> Locations { get; set; } = new List<SupplierLocation>();
    public ICollection<SupplierOffer> Offers { get; set; } = new List<SupplierOffer>();
    public ICollection<SupplierOrder> SupplierOrders { get; set; } = new List<SupplierOrder>();
}
