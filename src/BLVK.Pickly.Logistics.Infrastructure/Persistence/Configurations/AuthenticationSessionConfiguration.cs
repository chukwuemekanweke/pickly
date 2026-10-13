using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class AuthenticationSessionConfiguration : IEntityTypeConfiguration<AuthenticationSession>
{
    public void Configure(EntityTypeBuilder<AuthenticationSession> builder)
    {
        builder.ToTable("Sessions", SchemaNames.Authentication);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.DeviceName).HasMaxLength(200);
        builder.Property(x => x.DevicePlatform).HasMaxLength(100);
        builder.Property(x => x.BrowserName).HasMaxLength(100);
        builder.HasOne(x => x.Stakeholder).WithMany().HasForeignKey(x => x.StakeholderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FirstIpAddress).WithMany().HasForeignKey(x => x.FirstIpAddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LastIpAddress).WithMany().HasForeignKey(x => x.LastIpAddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StakeholderId);
        builder.HasIndex(x => x.LastActiveAtUtc);
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
