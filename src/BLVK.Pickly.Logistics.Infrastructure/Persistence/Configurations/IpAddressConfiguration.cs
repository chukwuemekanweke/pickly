using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class IpAddressConfiguration : IEntityTypeConfiguration<IpAddress>
{
    public void Configure(EntityTypeBuilder<IpAddress> builder)
    {
        builder.ToTable("IpAddresses", SchemaNames.Authentication);
        builder.Property(ipAddress => ipAddress.Value).HasMaxLength(45);

        builder
            .HasMany(ipAddress => ipAddress.Locations)
            .WithOne(ipAddressLocation => ipAddressLocation.IpAddress)
            .HasForeignKey(ipAddressLocation => ipAddressLocation.IpAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(ipAddress => ipAddress.Locations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(ipAddress => ipAddress.Value).IsUnique();
        builder.HasIndex(ipAddress => ipAddress.LocationLookupTimestampUtc);
    }
}
