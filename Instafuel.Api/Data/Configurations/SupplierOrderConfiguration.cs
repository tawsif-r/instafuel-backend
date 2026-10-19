using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class SupplierOrderConfiguration
    : IEntityTypeConfiguration<SupplierOrder>
{
    public void Configure(EntityTypeBuilder<SupplierOrder> builder)
    {
        builder.ToTable("supplier_orders", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_orders_status",
                "status IN ('pending', 'accepted', 'rejected', 'preparing', 'ready', 'dispatched', 'delivered', 'cancelled')");

            table.HasCheckConstraint(
                "ck_supplier_orders_subtotal",
                "subtotal >= 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

        builder.Property(s => s.Subtotal)
            .HasPrecision(14, 2)
            .IsRequired();

        builder.HasIndex(s => s.OrderId);

        builder.HasIndex(s => s.SupplierId);

        builder.HasIndex(s => s.SupplierLocationId);

        builder.HasIndex(s => s.Status);

        builder.HasOne(s => s.Order)
            .WithMany(o => o.SupplierOrders)
            .HasForeignKey(s => s.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Supplier)
            .WithMany(s => s.SupplierOrders)
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SupplierLocation)
            .WithMany(l => l.SupplierOrders)
            .HasForeignKey(s => s.SupplierLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}