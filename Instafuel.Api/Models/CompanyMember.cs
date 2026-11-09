namespace Instafuel.Api.Models;

public class CompanyMember
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // member | manager
    public string Role { get; set; } = "member";

    // pending | active | rejected
    public string Status { get; set; } = "pending";

    public decimal CreditLimit { get; set; } = 50000m;
    public decimal UsedCredit { get; set; } = 0m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

