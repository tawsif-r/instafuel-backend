using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Instafuel.Api.Data.Configurations;

public sealed class EmailChangeRequestConfiguration : IEntityTypeConfiguration<EmailChangeRequest>
{
    public void Configure(EntityTypeBuilder<EmailChangeRequest> builder)
    {
        builder.ToTable("email_change_requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.NewEmail).IsRequired().HasMaxLength(255);
        builder.Property(request => request.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(request => request.TokenHash).IsUnique();
        builder.HasIndex(request => new { request.UserId, request.ExpiresAt });
        builder.HasOne(request => request.User).WithMany().HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
