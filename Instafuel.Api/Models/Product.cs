namespace Instafuel.Api.Models;

public class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProductType { get; set; } = string.Empty; // fuel | general
    public string UnitCode { get; set; } = string.Empty;    // L | KG | EA ...
    public string? FuelGrade { get; set; }                  // required when ProductType = fuel
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
    public ICollection<SupplierOffer> Offers { get; set; } = new List<SupplierOffer>();
}
