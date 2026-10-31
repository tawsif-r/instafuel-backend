using Microsoft.EntityFrameworkCore;
using Instafuel.Api.Data.Configurations;

namespace Instafuel.Api.Models;

public class PgDbContext : DbContext
{
    public PgDbContext(DbContextOptions<PgDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<EmailChangeRequest> EmailChangeRequests => Set<EmailChangeRequest>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<CustomerVehicle> CustomerVehicles => Set<CustomerVehicle>();
    public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();
    public DbSet<RiderPreferredArea> RiderPreferredAreas => Set<RiderPreferredArea>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierLocation> SupplierLocations => Set<SupplierLocation>();
    public DbSet<SupplierMember> SupplierMembers => Set<SupplierMember>();
    public DbSet<SupplierOffer> SupplierOffers => Set<SupplierOffer>();
    public DbSet<SupplierOrder> SupplierOrders => Set<SupplierOrder>();
    public DbSet<SupplierOrderStatusHistory> SupplierOrderStatusHistories => Set<SupplierOrderStatusHistory>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistories => Set<DeliveryStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PgDbContext).Assembly);

        // Keep property column names explicit to match the existing PostgreSQL schema.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        var result = new System.Text.StringBuilder(name.Length + 5);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current) && i > 0)
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(current));
        }

        return result.ToString();
    }
}
