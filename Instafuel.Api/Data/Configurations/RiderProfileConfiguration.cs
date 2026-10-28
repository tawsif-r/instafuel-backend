using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class RiderProfileConfiguration : IEntityTypeConfiguration<RiderProfile>
{
    public void Configure(EntityTypeBuilder<RiderProfile> builder)
    {
        builder.ToTable("rider_profiles", table =>
        {
            table.HasCheckConstraint("ck_rider_profiles_verification_status",
                "verification_status IN ('pending', 'verified', 'rejected', 'suspended')");
            table.HasCheckConstraint("ck_rider_profiles_vehicle_type_length",
                "vehicle_type IS NULL OR length(vehicle_type) <= 50");
            table.HasCheckConstraint("ck_rider_profiles_registration_length",
                "vehicle_registration_number IS NULL OR length(vehicle_registration_number) <= 50");
        });
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.VerificationStatus).HasMaxLength(30).IsRequired().HasDefaultValue("pending");
        builder.Property(profile => profile.VehicleType).HasMaxLength(50);
        builder.Property(profile => profile.VehicleRegistrationNumber).HasMaxLength(50);
        builder.HasIndex(profile => profile.UserId).IsUnique();
        builder.HasOne(profile => profile.User).WithOne(user => user.RiderProfile)
            .HasForeignKey<RiderProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
