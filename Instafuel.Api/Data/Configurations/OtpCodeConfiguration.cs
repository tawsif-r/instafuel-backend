using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes", table =>
        {
            table.HasCheckConstraint("ck_otp_codes_attempts", "attempts >= 0 AND attempts <= 5");
            table.HasCheckConstraint("ck_otp_codes_purpose", "purpose IN ('signup', 'login', 'reset')");
        });
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Phone).IsRequired().HasMaxLength(30);
        builder.Property(code => code.Purpose).IsRequired().HasMaxLength(20);
        builder.Property(code => code.CodeHash).IsRequired().HasMaxLength(64);
        builder.Property(code => code.Attempts).HasDefaultValue(0);
        builder.HasIndex(code => new { code.Phone, code.Purpose, code.CreatedAt });
        builder.HasIndex(code => new { code.UserId, code.Purpose, code.ExpiresAt });
        builder.HasIndex(code => code.ExpiresAt);
        builder.HasOne(code => code.User).WithMany().HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
