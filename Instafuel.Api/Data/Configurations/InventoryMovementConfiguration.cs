using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class InventoryMovementConfiguration
    : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_movements_type",
                "movement_type IN ('receipt', 'reservation', 'release', 'sale', 'adjustment', 'return')");

            table.HasCheckConstraint(
                "ck_inventory_movements_quantity_delta",
                "quantity_delta <> 0");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(m => m.QuantityDelta)
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(m => m.ReferenceType)
            .HasMaxLength(50);

        builder.Property(m => m.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(m => m.InventoryBalanceId);

        builder.HasIndex(m => m.CreatedAt);

        builder.HasIndex(m => new
        {
            m.ReferenceType,
            m.ReferenceId
        });

        builder.HasOne(m => m.InventoryBalance)
            .WithMany(i => i.Movements)
            .HasForeignKey(m => m.InventoryBalanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}