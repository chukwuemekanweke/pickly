using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeviceAccountConfiguration : IEntityTypeConfiguration<DeviceAccount>
{
    public void Configure(EntityTypeBuilder<DeviceAccount> builder)
    {
        builder.ToTable("DeviceAccounts", SchemaNames.Authentication);
        builder.HasKey(deviceAccount => deviceAccount.Id);

        builder.Property(deviceAccount => deviceAccount.Identifier)
            .HasMaxLength(DeviceAccount.MaxIdentifierLength)
            .IsRequired();
        builder.Property(deviceAccount => deviceAccount.AppUserId).IsRequired();
        builder.Property(deviceAccount => deviceAccount.StakeholderId).IsRequired();

        builder.HasIndex(deviceAccount => deviceAccount.Identifier).IsUnique();
        builder.HasIndex(deviceAccount => deviceAccount.AppUserId).IsUnique();
        builder.HasIndex(deviceAccount => deviceAccount.StakeholderId).IsUnique();

        builder.HasOne<AppUser>()
            .WithOne()
            .HasForeignKey<DeviceAccount>(deviceAccount => deviceAccount.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Stakeholder>()
            .WithOne()
            .HasForeignKey<DeviceAccount>(deviceAccount => deviceAccount.StakeholderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
