using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class SupplierOfferConfiguration
    : IEntityTypeConfiguration<SupplierOffer>
{
    public void Configure(EntityTypeBuilder<SupplierOffer> builder)
    {
        builder.ToTable("supplier_offers", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_offers_unit_price",
                "unit_price >= 0");

            table.HasCheckConstraint(
                "ck_supplier_offers_status",
                "status IN ('active', 'paused', 'discontinued')");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UnitPrice)
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("BDT");

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("active");

        builder.Property(s => s.SellerSku)
            .HasMaxLength(100);

        builder.HasIndex(s => new { s.SupplierId, s.ProductId })
            .IsUnique();

        builder.HasIndex(s => s.SupplierId);

        builder.HasIndex(s => s.ProductId);

        builder.HasIndex(s => s.Status);

        builder.HasOne(s => s.Supplier)
            .WithMany(s => s.Offers)
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany(p => p.Offers)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}