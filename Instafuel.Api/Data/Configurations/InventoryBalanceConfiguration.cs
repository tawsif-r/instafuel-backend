using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class InventoryBalanceConfiguration
    : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("inventory_balances", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_balances_quantity_on_hand",
                "quantity_on_hand >= 0");

            table.HasCheckConstraint(
                "ck_inventory_balances_quantity_reserved",
                "quantity_reserved >= 0");

            table.HasCheckConstraint(
                "ck_inventory_balances_reserved_lte_on_hand",
                "quantity_reserved <= quantity_on_hand");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Version)
            .IsRowVersion();

        builder.Property(i => i.QuantityOnHand)
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(i => i.QuantityReserved)
            .HasPrecision(14, 3)
            .IsRequired();

        builder.HasIndex(i => new
        {
            i.SupplierLocationId,
            i.SupplierOfferId
        }).IsUnique();

        builder.HasIndex(i => i.SupplierOfferId);

        builder.HasOne(i => i.SupplierLocation)
            .WithMany(l => l.InventoryBalances)
            .HasForeignKey(i => i.SupplierLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.SupplierOffer)
            .WithMany(o => o.InventoryBalances)
            .HasForeignKey(i => i.SupplierOfferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}