using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", table =>
        {
            table.HasCheckConstraint(
                "ck_orders_status",
                "status IN ('pending', 'confirmed', 'processing', 'partially_fulfilled', 'completed', 'cancelled', 'failed')");

            table.HasCheckConstraint(
                "ck_orders_amounts_non_negative",
                "subtotal >= 0 AND delivery_fee >= 0 AND discount_amount >= 0 AND total_amount >= 0");

            table.HasCheckConstraint(
                "ck_orders_discount_limit",
                "discount_amount <= subtotal + delivery_fee");

            table.HasCheckConstraint(
                "ck_orders_total_amount",
                "total_amount = subtotal + delivery_fee - discount_amount");

            table.HasCheckConstraint(
                "ck_orders_latitude",
                "delivery_latitude IS NULL OR delivery_latitude BETWEEN -90 AND 90");

            table.HasCheckConstraint(
                "ck_orders_longitude",
                "delivery_longitude IS NULL OR delivery_longitude BETWEEN -180 AND 180");
        });

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("pending");

        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("BDT");

        builder.Property(o => o.Subtotal).HasPrecision(14, 2);
        builder.Property(o => o.DeliveryFee).HasPrecision(14, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(14, 2);
        builder.Property(o => o.TotalAmount).HasPrecision(14, 2);

        builder.Property(o => o.RecipientName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(o => o.RecipientPhone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(o => o.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.DeliveryCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CustomerNotes)
            .HasMaxLength(1000);

        builder.Property(o => o.DeliveryLatitude).HasPrecision(9, 6);
        builder.Property(o => o.DeliveryLongitude).HasPrecision(9, 6);

        builder.HasIndex(o => o.CustomerId);

        builder.HasIndex(o => o.Status);

        builder.HasIndex(o => o.CreatedAt);

        builder.HasOne(o => o.Customer)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}