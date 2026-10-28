namespace Instafuel.Api.Models;

public class CustomerVehicle
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Customer { get; set; } = null!;
}
