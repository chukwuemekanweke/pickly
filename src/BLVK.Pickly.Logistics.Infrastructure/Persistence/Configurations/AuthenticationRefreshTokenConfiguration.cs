using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class AuthenticationRefreshTokenConfiguration : IEntityTypeConfiguration<AuthenticationRefreshToken>
{
    public void Configure(EntityTypeBuilder<AuthenticationRefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", SchemaNames.Authentication);
        builder.Property(refreshToken => refreshToken.TokenHash).HasMaxLength(64);
        builder.Property(refreshToken => refreshToken.SecurityStamp).HasMaxLength(256);

        builder.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();
        builder.HasIndex(refreshToken => refreshToken.AppUserId);
        builder.HasIndex(refreshToken => refreshToken.AuthenticationSessionId);
        builder.HasOne(refreshToken => refreshToken.AuthenticationSession)
            .WithMany().HasForeignKey(refreshToken => refreshToken.AuthenticationSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(refreshToken => refreshToken.AppUser)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
