namespace Instafuel.Api.Models;

// A driver is a User who drives the company's fuel trucks.
// One user can have at most one driver profile.
public class Driver
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly LicenseExpiry { get; set; }
    public bool DangerousGoodsCertified { get; set; }         // allowed to carry fuel (hazardous goods)
    public string VerificationStatus { get; set; } = "pending"; // pending | verified | rejected
    public string EmploymentStatus { get; set; } = "active";    // active | on_leave | terminated
    public bool IsAvailable { get; set; } = true;              // false while on a delivery
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
