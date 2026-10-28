using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class CustomerVehicleConfiguration : IEntityTypeConfiguration<CustomerVehicle>
{
    public void Configure(EntityTypeBuilder<CustomerVehicle> builder)
    {
        builder.ToTable("customer_vehicles");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Label).HasMaxLength(80).IsRequired();
        builder.Property(vehicle => vehicle.VehicleType).HasMaxLength(50).IsRequired();
        builder.Property(vehicle => vehicle.Make).HasMaxLength(100);
        builder.Property(vehicle => vehicle.Model).HasMaxLength(100);
        builder.Property(vehicle => vehicle.RegistrationNumber).HasMaxLength(50);
        builder.HasIndex(vehicle => vehicle.CustomerId);
        builder.HasOne(vehicle => vehicle.Customer).WithMany(user => user.Vehicles)
            .HasForeignKey(vehicle => vehicle.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
