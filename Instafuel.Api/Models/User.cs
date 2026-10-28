namespace Instafuel.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<SupplierMember> SupplierMemberships { get; set; } = new List<SupplierMember>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Delivery> AssignedDeliveries { get; set; } = new List<Delivery>();
    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public ICollection<CustomerVehicle> Vehicles { get; set; } = new List<CustomerVehicle>();
}
