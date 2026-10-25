using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint(
                "ck_products_name_not_empty",
                "length(trim(name)) > 0");

            table.HasCheckConstraint(
                "ck_products_type",
                "product_type IN ('fuel', 'general')");

            table.HasCheckConstraint(
                "ck_products_unit_code_not_empty",
                "length(trim(unit_code)) > 0");

            table.HasCheckConstraint(
                "ck_products_fuel_grade_required",
                "product_type <> 'fuel' OR " +
                "(fuel_grade IS NOT NULL AND length(trim(fuel_grade)) > 0)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.ProductType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.UnitCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.FuelGrade)
            .HasMaxLength(50);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(p => p.CategoryId);

        builder.HasIndex(p => p.ProductType);

        builder.HasIndex(p => p.Name);
    }
}