using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Models;

public class PgDbContext : DbContext
{
    public PgDbContext(DbContextOptions<PgDbContext> options)
        : base(options)
    {

    }

    public DbSet<Test> Tests { get; set; }

    // Users & suppliers
    public DbSet<User> Users { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierMember> SupplierMembers { get; set; }
    public DbSet<SupplierLocation> SupplierLocations { get; set; }

    // Customer vehicles
    public DbSet<CustomerVehicle> CustomerVehicles { get; set; }
    public DbSet<CustomerVehicleDocument> CustomerVehicleDocuments { get; set; }

    // Products & pricing
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<SupplierOffer> SupplierOffers { get; set; }

    // Inventory
    public DbSet<InventoryBalance> InventoryBalances { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }

    // Orders & payments
    public DbSet<Order> Orders { get; set; }
    public DbSet<SupplierOrder> SupplierOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Refund> Refunds { get; set; }

    // Fleet & delivery
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistories { get; set; }

    // Constraints that attributes can't express (unique indexes, check constraints,
    // delete rules) live here. Decimal precision stays as [Precision] on the models.
    //
    // Delete rule convention:
    //   Restrict -> business records we must never lose by accident (orders, payments, deliveries...)
    //   Cascade  -> child rows that mean nothing without their parent (order items, history rows)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- User ----------
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            e.Property(u => u.Email).HasMaxLength(255);
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.Role).HasMaxLength(30);
            e.Property(u => u.AccountType).HasMaxLength(30);
            // Unique, but NULLs are allowed (Postgres treats NULLs as distinct).
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Phone).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_User_Role", "\"Role\" IN ('customer', 'driver', 'supplier_staff', 'admin')");
                t.HasCheckConstraint("CK_User_AccountType", "\"AccountType\" IN ('individual', 'corporate')");
            });
        });

        // ---------- Customer vehicles ----------
        modelBuilder.Entity<CustomerVehicle>(e =>
        {
            e.Property(v => v.Nickname).HasMaxLength(60).IsRequired();
            e.Property(v => v.VehicleType).HasMaxLength(20);
            e.Property(v => v.RegistrationNumber).HasMaxLength(30).IsRequired();
            e.Property(v => v.FuelType).HasMaxLength(20);
            e.Property(v => v.VerificationStatus).HasMaxLength(20);
            e.Property(v => v.RejectionReason).HasMaxLength(300);
            // One live registration per plate across all customers. Deleted (inactive)
            // vehicles don't count, so a sold car can be registered by its new owner.
            e.HasIndex(v => v.RegistrationNumber).IsUnique().HasFilter("\"IsActive\"");
            e.HasOne(v => v.User).WithMany(u => u.CustomerVehicles)
                .HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CustomerVehicle_Type",
                    "\"VehicleType\" IN ('car', 'motorcycle', 'microbus', 'pickup', 'truck', 'generator')");
                t.HasCheckConstraint("CK_CustomerVehicle_FuelType", "\"FuelType\" IN ('octane', 'petrol', 'diesel')");
                t.HasCheckConstraint("CK_CustomerVehicle_VerificationStatus",
                    "\"VerificationStatus\" IN ('pending', 'verified', 'rejected')");
            });
        });

        modelBuilder.Entity<CustomerVehicleDocument>(e =>
        {
            e.Property(d => d.Kind).HasMaxLength(30);
            e.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            e.HasIndex(d => new { d.CustomerVehicleId, d.Kind }).IsUnique(); // one document per kind
            e.HasOne(d => d.CustomerVehicle).WithMany(v => v.Documents)
                .HasForeignKey(d => d.CustomerVehicleId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t => t.HasCheckConstraint("CK_CustomerVehicleDocument_Kind",
                "\"Kind\" IN ('registration_certificate', 'fitness_certificate', 'number_plate_photo')"));
        });

        // ---------- Supplier ----------
        modelBuilder.Entity<Supplier>(e =>
        {
            e.Property(s => s.LegalName).HasMaxLength(200).IsRequired();
            e.Property(s => s.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(s => s.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<SupplierMember>(e =>
        {
            e.Property(m => m.MemberRole).HasMaxLength(20);
            e.HasOne(m => m.Supplier).WithMany(s => s.Members)
                .HasForeignKey(m => m.SupplierId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany(u => u.SupplierMemberships)
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierLocation>(e =>
        {
            e.Property(l => l.Name).HasMaxLength(150).IsRequired();
            e.Property(l => l.City).HasMaxLength(100);
            e.HasOne(l => l.Supplier).WithMany(s => s.Locations)
                .HasForeignKey(l => l.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Category & Product ----------
        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Slug).IsUnique();
            // Self reference: can't delete a parent that still has children.
            e.HasOne(c => c.Parent).WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(150).IsRequired();
            e.Property(p => p.ProductType).HasMaxLength(20);
            e.Property(p => p.UnitCode).HasMaxLength(10);
            e.Property(p => p.FuelGrade).HasMaxLength(50);
            e.HasOne(p => p.Category).WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierOffer>(e =>
        {
            e.Property(o => o.Currency).HasMaxLength(3);
            e.Property(o => o.Status).HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_SupplierOffer_UnitPrice", "\"UnitPrice\" >= 0"));
            e.HasOne(o => o.Supplier).WithMany(s => s.Offers)
                .HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Product).WithMany(p => p.Offers)
                .HasForeignKey(o => o.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Inventory ----------
        modelBuilder.Entity<InventoryBalance>(e =>
        {
            // One balance row per (location, offer).
            e.HasIndex(b => new { b.SupplierLocationId, b.SupplierOfferId }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_InventoryBalance_OnHand", "\"QuantityOnHand\" >= 0");
                t.HasCheckConstraint("CK_InventoryBalance_Reserved", "\"QuantityReserved\" >= 0");
            });
            e.HasOne(b => b.SupplierLocation).WithMany(l => l.InventoryBalances)
                .HasForeignKey(b => b.SupplierLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.SupplierOffer).WithMany(o => o.InventoryBalances)
                .HasForeignKey(b => b.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryMovement>(e =>
        {
            e.Property(m => m.MovementType).HasMaxLength(20).IsRequired();
            e.HasOne(m => m.InventoryBalance).WithMany(b => b.Movements)
                .HasForeignKey(m => m.InventoryBalanceId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Orders ----------
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.Status).HasMaxLength(30);
            e.Property(o => o.Currency).HasMaxLength(3);
            e.Property(o => o.RecipientName).HasMaxLength(150).IsRequired();
            e.Property(o => o.RecipientPhone).HasMaxLength(20).IsRequired();
            e.Property(o => o.DeliveryAddress).IsRequired();
            e.Property(o => o.DeliveryCity).HasMaxLength(100).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Order_TotalAmount", "\"TotalAmount\" >= 0"));
            e.HasOne(o => o.Customer).WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierOrder>(e =>
        {
            e.Property(s => s.Status).HasMaxLength(20);
            e.HasOne(s => s.Order).WithMany(o => o.SupplierOrders)
                .HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Supplier).WithMany(s => s.SupplierOrders)
                .HasForeignKey(s => s.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.SupplierLocation).WithMany(l => l.SupplierOrders)
                .HasForeignKey(s => s.SupplierLocationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.ProductName).HasMaxLength(150).IsRequired();
            e.Property(i => i.SellerName).HasMaxLength(200).IsRequired();
            e.Property(i => i.UnitCode).HasMaxLength(10);
            e.Property(i => i.VehicleName).HasMaxLength(60);
            e.Property(i => i.VehicleRegistration).HasMaxLength(30);
            e.Property(i => i.FuelType).HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_OrderItem_Quantity", "\"Quantity\" > 0"));
            e.HasOne(i => i.SupplierOrder).WithMany(s => s.Items)
                .HasForeignKey(i => i.SupplierOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.SupplierOffer).WithMany(o => o.OrderItems)
                .HasForeignKey(i => i.SupplierOfferId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.CustomerVehicle).WithMany()
                .HasForeignKey(i => i.CustomerVehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Payments ----------
        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Currency).HasMaxLength(3);
            e.Property(p => p.Method).HasMaxLength(20).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20);
            // Same key twice = same payment attempt, so it must be unique.
            e.Property(p => p.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.HasIndex(p => p.IdempotencyKey).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "\"Amount\" > 0"));
            e.HasOne(p => p.Order).WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Refund>(e =>
        {
            e.Property(r => r.Status).HasMaxLength(20);
            e.ToTable(t => t.HasCheckConstraint("CK_Refund_Amount", "\"Amount\" > 0"));
            e.HasOne(r => r.Payment).WithMany(p => p.Refunds)
                .HasForeignKey(r => r.PaymentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Fleet: Driver & Vehicle ----------
        modelBuilder.Entity<Driver>(e =>
        {
            e.Property(d => d.LicenseNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(d => d.LicenseNumber).IsUnique();
            e.Property(d => d.VerificationStatus).HasMaxLength(20);
            e.Property(d => d.EmploymentStatus).HasMaxLength(20);
            // One user -> at most one driver profile (unique FK makes it 1-to-0..1).
            e.HasIndex(d => d.UserId).IsUnique();
            e.HasOne(d => d.User).WithOne(u => u.Driver)
                .HasForeignKey<Driver>(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.RegistrationNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(v => v.RegistrationNumber).IsUnique();
            e.Property(v => v.VehicleType).HasMaxLength(20);
            e.Property(v => v.Status).HasMaxLength(20);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Vehicle_Capacity", "\"CapacityLitres\" > 0");
                t.HasCheckConstraint("CK_Vehicle_Compartments", "\"CompartmentCount\" >= 1");
            });
        });

        // ---------- Delivery ----------
        modelBuilder.Entity<Delivery>(e =>
        {
            e.Property(d => d.Status).HasMaxLength(20);
            e.Property(d => d.FailureReason).HasMaxLength(500);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Delivery_DeliveredQuantity", "\"DeliveredQuantity\" IS NULL OR \"DeliveredQuantity\" >= 0"));
            e.HasOne(d => d.SupplierOrder).WithMany(s => s.Deliveries)
                .HasForeignKey(d => d.SupplierOrderId).OnDelete(DeleteBehavior.Restrict);
            // Keep delivery history even if a driver/vehicle record is removed later.
            e.HasOne(d => d.Driver).WithMany(dr => dr.Deliveries)
                .HasForeignKey(d => d.DriverId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Vehicle).WithMany(v => v.Deliveries)
                .HasForeignKey(d => d.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeliveryStatusHistory>(e =>
        {
            e.Property(h => h.Status).HasMaxLength(20).IsRequired();
            e.HasOne(h => h.Delivery).WithMany(d => d.StatusHistory)
                .HasForeignKey(h => h.DeliveryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.RecordedByUser).WithMany()
                .HasForeignKey(h => h.RecordedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
