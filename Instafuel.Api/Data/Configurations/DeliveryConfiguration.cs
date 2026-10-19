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
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

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