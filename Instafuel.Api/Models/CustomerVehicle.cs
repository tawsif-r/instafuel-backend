using System.Text.Json.Serialization;

namespace Instafuel.Api.Models;

// A customer's own vehicle (car, generator...). Orders may only include verified ones,
// and the driver matches RegistrationNumber against the plate before fuelling.
// Not the same as Vehicle, which is a truck in our delivery fleet.
public class CustomerVehicle
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "car"; // car | motorcycle | microbus | pickup | truck | generator

    // Stored normalized (trimmed, uppercase, single spaces) so duplicates are caught.
    public string RegistrationNumber { get; set; } = string.Empty;

    public string FuelType { get; set; } = "octane";              // octane | petrol | diesel
    public string VerificationStatus { get; set; } = "pending";   // pending | verified | rejected
    public string? RejectionReason { get; set; }                  // shown to the customer when rejected
    public DateTime? VerifiedAt { get; set; }
    public bool IsActive { get; set; } = true;                    // false = deleted by the customer
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore] // don't leak owner details in vehicle responses
    public User User { get; set; } = null!;

    public ICollection<CustomerVehicleDocument> Documents { get; set; } = new List<CustomerVehicleDocument>();
}
