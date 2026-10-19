using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class PgDbContext : DbContext
{
    public PgDbContext(DbContextOptions<PgDbContext> options)
        : base(options)
    {
    }


    public DbSet<User> Users { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierMember> SupplierMembers { get; set; }
    public DbSet<SupplierLocation> SupplierLocations { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<SupplierOffer> SupplierOffers { get; set; }
    public DbSet<InventoryBalance> InventoryBalances { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<SupplierOrder> SupplierOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PgDbContext).Assembly
        );
    }
}