using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers", table =>
        {
            table.HasCheckConstraint(
                "ck_suppliers_status",
                "status IN ('pending', 'active', 'suspended', 'rejected')");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.LegalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

        builder.HasIndex(s => s.Status);
    }
}