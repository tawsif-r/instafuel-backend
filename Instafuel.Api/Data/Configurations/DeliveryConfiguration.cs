using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries", table =>
        {
            table.HasCheckConstraint(
                "ck_deliveries_status",
                "status IN ('pending', 'assigned', 'picked_up', 'in_transit', 'delivered', 'failed', 'cancelled')");

            table.HasCheckConstraint(
                "ck_deliveries_timestamps",
                "(picked_up_at IS NULL OR assigned_at IS NOT NULL) AND " +
                "(delivered_at IS NULL OR picked_up_at IS NOT NULL)");
            table.HasCheckConstraint("ck_deliveries_cash_collected_non_negative", "cash_collected_amount >= 0");
            table.HasCheckConstraint("ck_deliveries_rider_latitude", "rider_latitude IS NULL OR rider_latitude BETWEEN -90 AND 90");
            table.HasCheckConstraint("ck_deliveries_rider_longitude", "rider_longitude IS NULL OR rider_longitude BETWEEN -180 AND 180");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

        builder.Property(d => d.ProofOfDeliveryReference).HasMaxLength(500);
        builder.Property(d => d.SafetyChecklistNotes).HasMaxLength(1000);
        builder.Property(d => d.SafetyChecklistJson).HasColumnType("jsonb");
        builder.Property(d => d.CashCollectedAmount).HasPrecision(14, 2).HasDefaultValue(0m);
        builder.Property(d => d.RiderLatitude).HasPrecision(9, 6);
        builder.Property(d => d.RiderLongitude).HasPrecision(9, 6);

        builder.HasIndex(d => d.SupplierOrderId);

        builder.HasIndex(d => d.DeliveryAgentId);

        builder.HasIndex(d => d.Status);

        builder.HasOne(d => d.SupplierOrder)
            .WithMany(s => s.Deliveries)
            .HasForeignKey(d => d.SupplierOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DeliveryAgent)
            .WithMany(u => u.AssignedDeliveries)
            .HasForeignKey(d => d.DeliveryAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
