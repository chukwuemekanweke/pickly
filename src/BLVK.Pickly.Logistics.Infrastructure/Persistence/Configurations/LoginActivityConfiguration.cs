using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class LoginActivityConfiguration : IEntityTypeConfiguration<LoginActivity>
{
    public void Configure(EntityTypeBuilder<LoginActivity> builder)
    {
        builder.ToTable("LoginActivities", SchemaNames.Authentication);
        builder.Property(loginActivity => loginActivity.UserAgent).HasMaxLength(500);
        builder.Property(loginActivity => loginActivity.DeviceName).HasMaxLength(200);
        builder.Property(loginActivity => loginActivity.DevicePlatform).HasMaxLength(100);
        builder.Property(loginActivity => loginActivity.BrowserName).HasMaxLength(100);

        builder
            .HasOne(loginActivity => loginActivity.IpAddress)
            .WithMany()
            .HasForeignKey(loginActivity => loginActivity.IpAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(loginActivity => loginActivity.IpAddressLocation)
            .WithMany()
            .HasForeignKey(loginActivity => loginActivity.IpAddressLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(loginActivity => loginActivity.StakeholderId);
        builder.HasIndex(loginActivity => loginActivity.TenantId);
        builder.HasIndex(loginActivity => loginActivity.OccurredAtUtc);
        builder.HasIndex(loginActivity => loginActivity.IpAddressId);
        builder.HasIndex(loginActivity => loginActivity.IpAddressLocationId);
        builder.HasIndex(loginActivity => new
        {
            loginActivity.TenantId,
            loginActivity.StakeholderId,
            loginActivity.OccurredAtUtc,
            loginActivity.Id
        });
    }
}
