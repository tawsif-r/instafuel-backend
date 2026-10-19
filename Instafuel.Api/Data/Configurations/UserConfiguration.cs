using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Instafuel.Api.Models;

namespace Instafuel.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint(
                "ck_users_full_name_not_empty",
                "length(trim(full_name)) > 0");
        });

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        builder.HasIndex(u => u.Phone)
            .IsUnique()
            .HasFilter("phone IS NOT NULL");

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .HasMaxLength(255);

        builder.Property(u => u.Phone)
            .HasMaxLength(30);

        builder.Property(u => u.PasswordHash);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);
    }
}