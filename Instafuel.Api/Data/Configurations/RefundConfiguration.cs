using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds", table =>
        {
            table.HasCheckConstraint(
                "ck_refunds_amount",
                "amount > 0");

            table.HasCheckConstraint(
                "ck_refunds_status",
                "status IN ('pending', 'successful', 'failed', 'cancelled')");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount)
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

        builder.Property(r => r.ProviderReference)
            .HasMaxLength(200);

        builder.HasIndex(r => r.PaymentId);

        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}