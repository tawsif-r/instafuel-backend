using System.Text.Json.Serialization;

namespace Instafuel.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    [JsonIgnore] // never send the hash back in API responses
    public string? PasswordHash { get; set; }

    // customer | driver | supplier_staff | admin. Goes into the JWT role claim.
    public string Role { get; set; } = "customer";

    // individual | corporate. Company links themselves come later (no company table yet).
    public string AccountType { get; set; } = "individual";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierMember> SupplierMemberships { get; set; } = new List<SupplierMember>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CustomerVehicle> CustomerVehicles { get; set; } = new List<CustomerVehicle>();
    public Driver? Driver { get; set; } // only set when this user works as a driver
}
