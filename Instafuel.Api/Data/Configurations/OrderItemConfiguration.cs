using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", table =>
        {
            table.HasCheckConstraint(
                "ck_order_items_quantity",
                "quantity > 0");

            table.HasCheckConstraint(
                "ck_order_items_unit_price",
                "unit_price >= 0");

            table.HasCheckConstraint(
                "ck_order_items_line_total",
                "line_total = round(quantity * unit_price, 2)");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(i => i.SellerName)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(i => i.Quantity)
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(i => i.UnitCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.UnitPrice)
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(i => i.LineTotal)
            .HasPrecision(14, 2)
            .IsRequired();

        builder.HasIndex(i => i.SupplierOrderId);

        builder.HasIndex(i => i.SupplierOfferId);

        builder.HasOne(i => i.SupplierOrder)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SupplierOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.SupplierOffer)
            .WithMany(s => s.OrderItems)
            .HasForeignKey(i => i.SupplierOfferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}