using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> builder)
    {
        builder.ToTable("password_reset_requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(request => request.TokenHash).IsUnique();
        builder.HasIndex(request => new { request.UserId, request.ExpiresAt });
        builder.HasOne(request => request.User).WithMany().HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
