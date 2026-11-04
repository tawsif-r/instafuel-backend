using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

// A truck in the company's own delivery fleet. Not owned by any user.
public class Vehicle
{
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "tanker"; // tanker | bowser | pickup

    [Precision(10, 2)]
    public decimal CapacityLitres { get; set; }

    public int CompartmentCount { get; set; } = 1; // separate tanks, lets one truck carry several fuel types
    public DateOnly? FitnessCertificateExpiry { get; set; }
    public DateOnly? LastInspectionDate { get; set; }
    public string Status { get; set; } = "active"; // active | under_maintenance | retired
    public bool IsAvailable { get; set; } = true;  // false while on a delivery
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
