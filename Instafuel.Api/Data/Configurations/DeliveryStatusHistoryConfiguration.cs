using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class DeliveryStatusHistoryConfiguration
    : IEntityTypeConfiguration<DeliveryStatusHistory>
{
    public void Configure(EntityTypeBuilder<DeliveryStatusHistory> builder)
    {
        builder.ToTable("delivery_status_histories", table =>
        {
            table.HasCheckConstraint(
                "ck_delivery_status_histories_status",
                "status IN ('pending', 'assigned', 'picked_up', 'in_transit', 'delivered', 'failed', 'cancelled')");
        });

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(h => h.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(h => h.DeliveryId);

        builder.HasIndex(h => h.RecordedByUserId);

        builder.HasIndex(h => h.CreatedAt);

        builder.HasOne(h => h.Delivery)
            .WithMany(d => d.StatusHistory)
            .HasForeignKey(h => h.DeliveryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.RecordedByUser)
            .WithMany()
            .HasForeignKey(h => h.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}