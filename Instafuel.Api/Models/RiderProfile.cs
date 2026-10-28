namespace Instafuel.Api.Models;

public class RiderProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string VerificationStatus { get; set; } = "pending"; // pending | verified | rejected | suspended
    public bool IsAvailable { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleRegistrationNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<RiderPreferredArea> PreferredAreas { get; set; } = new List<RiderPreferredArea>();
}
