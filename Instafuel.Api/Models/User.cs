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

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierMember> SupplierMemberships { get; set; } = new List<SupplierMember>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Driver? Driver { get; set; } // only set when this user works as a driver
}
