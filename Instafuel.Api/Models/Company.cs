namespace Instafuel.Api.Models;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g. "NORTHWIND", "INSTAFUEL"
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public decimal CreditLimit { get; set; } = 50000m;
    public decimal UsedCredit { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CompanyMember> Members { get; set; } = new List<CompanyMember>();
}

