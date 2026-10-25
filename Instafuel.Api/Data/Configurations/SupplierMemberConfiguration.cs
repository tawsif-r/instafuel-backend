using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class SupplierMemberConfiguration
    : IEntityTypeConfiguration<SupplierMember>
{
    public void Configure(EntityTypeBuilder<SupplierMember> builder)
    {
        builder.ToTable("supplier_members", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_members_role",
                "member_role IN ('owner', 'manager', 'staff')");
        });

        builder.HasKey(m => new
        {
            m.SupplierId,
            m.UserId
        });

        builder.Property(m => m.MemberRole)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("staff");

        builder.HasIndex(m => m.UserId);

        builder.HasIndex(m => m.MemberRole);

        builder.HasOne(m => m.Supplier)
            .WithMany(s => s.Members)
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
            .WithMany(u => u.SupplierMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}