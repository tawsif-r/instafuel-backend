using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", table =>
        {
            table.HasCheckConstraint(
                "ck_payments_amount",
                "amount > 0");

            table.HasCheckConstraint(
                "ck_payments_method",
                "method IN ('cash', 'card', 'mobile_banking', 'bank_transfer')");

            table.HasCheckConstraint(
                "ck_payments_status",
                "status IN ('pending', 'successful', 'failed', 'cancelled')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("BDT");

        builder.Property(p => p.Method)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

        builder.Property(p => p.ProviderReference)
            .HasMaxLength(200);

        builder.Property(p => p.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key <> ''");

        builder.HasIndex(p => p.OrderId, "IX_Payments_OrderId")
            .HasDatabaseName("ix_payments_order_id");

        builder.HasIndex(p => p.OrderId, "IX_Payments_CashActiveOrderId")
            .IsUnique()
            .HasDatabaseName("ix_payments_cash_active_order_id")
            .HasFilter("method = 'cash' AND status IN ('pending', 'successful')");

        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
