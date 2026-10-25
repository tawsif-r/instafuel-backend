using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class SupplierLocationConfiguration
    : IEntityTypeConfiguration<SupplierLocation>
{
    public void Configure(EntityTypeBuilder<SupplierLocation> builder)
    {
        builder.ToTable("supplier_locations", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_locations_name_not_empty",
                "length(trim(name)) > 0");

            table.HasCheckConstraint(
                "ck_supplier_locations_latitude",
                "latitude IS NULL OR latitude BETWEEN -90 AND 90");

            table.HasCheckConstraint(
                "ck_supplier_locations_longitude",
                "longitude IS NULL OR longitude BETWEEN -180 AND 180");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(l => l.AddressLine)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Latitude)
            .HasPrecision(9, 6);

        builder.Property(l => l.Longitude)
            .HasPrecision(9, 6);

        builder.Property(l => l.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(l => l.SupplierId);

        builder.HasIndex(l => new { l.City, l.IsActive });

        builder.HasOne(l => l.Supplier)
            .WithMany(s => s.Locations)
            .HasForeignKey(l => l.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.InventoryBalances)
            .WithOne(i => i.SupplierLocation)
            .HasForeignKey(i => i.SupplierLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.SupplierOrders)
            .WithOne(s => s.SupplierLocation)
            .HasForeignKey(s => s.SupplierLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}