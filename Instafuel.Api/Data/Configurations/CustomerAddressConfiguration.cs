using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("customer_addresses", table =>
        {
            table.HasCheckConstraint("ck_customer_addresses_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
            table.HasCheckConstraint("ck_customer_addresses_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
        });
        builder.HasKey(address => address.Id);
        builder.Property(address => address.Label).HasMaxLength(50).IsRequired();
        builder.Property(address => address.RecipientName).HasMaxLength(150).IsRequired();
        builder.Property(address => address.RecipientPhone).HasMaxLength(30).IsRequired();
        builder.Property(address => address.AddressLine).HasMaxLength(500).IsRequired();
        builder.Property(address => address.City).HasMaxLength(100).IsRequired();
        builder.Property(address => address.Latitude).HasPrecision(9, 6);
        builder.Property(address => address.Longitude).HasPrecision(9, 6);
        builder.HasIndex(address => address.CustomerId);
        builder.HasIndex(address => address.CustomerId).IsUnique().HasFilter("is_default");
        builder.HasOne(address => address.Customer).WithMany(user => user.Addresses)
            .HasForeignKey(address => address.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
