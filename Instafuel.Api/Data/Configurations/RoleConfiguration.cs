using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever(); // Fixed IDs seeded in migration

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        // Seed the fixed roles
        builder.HasData(
            new Role { Id = 1, Name = RoleNames.Customer },
            new Role { Id = 2, Name = RoleNames.Rider },
            new Role { Id = 3, Name = RoleNames.SupplierMember },
            new Role { Id = 4, Name = RoleNames.SectorSupervisor },
            new Role { Id = 5, Name = RoleNames.Admin },
            new Role { Id = 6, Name = RoleNames.SuperAdmin }
        );
    }
}

