using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class RiderPreferredAreaConfiguration : IEntityTypeConfiguration<RiderPreferredArea>
{
    public void Configure(EntityTypeBuilder<RiderPreferredArea> builder)
    {
        builder.ToTable("rider_preferred_areas", table =>
            table.HasCheckConstraint("ck_rider_preferred_areas_city_not_empty", "length(trim(city)) > 0"));
        builder.HasKey(area => area.Id);
        builder.Property(area => area.City).HasMaxLength(100).IsRequired();
        builder.HasIndex(area => new { area.RiderProfileId, area.City }).IsUnique();
        builder.HasOne(area => area.RiderProfile).WithMany(profile => profile.PreferredAreas)
            .HasForeignKey(area => area.RiderProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}
