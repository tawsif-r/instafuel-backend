using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class SupplierOrderStatusHistoryConfiguration : IEntityTypeConfiguration<SupplierOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<SupplierOrderStatusHistory> builder)
    {
        builder.ToTable("supplier_order_status_histories", table => table.HasCheckConstraint(
            "ck_supplier_order_status_histories_status",
            "status IN ('pending', 'accepted', 'rejected', 'preparing', 'ready', 'dispatched', 'delivered', 'cancelled')"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Status).IsRequired().HasMaxLength(30);
        builder.Property(item => item.Notes).HasMaxLength(1000);
        builder.HasIndex(item => new { item.SupplierOrderId, item.CreatedAt });
        builder.HasOne(item => item.SupplierOrder).WithMany(item => item.StatusHistory)
            .HasForeignKey(item => item.SupplierOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.RecordedByUser).WithMany()
            .HasForeignKey(item => item.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
