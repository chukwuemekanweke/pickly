using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class IpAddressLocationConfiguration : IEntityTypeConfiguration<IpAddressLocation>
{
    public void Configure(EntityTypeBuilder<IpAddressLocation> builder)
    {
        builder.ToTable("IpAddressLocations", SchemaNames.Authentication);
        builder.Property(ipAddressLocation => ipAddressLocation.City).HasMaxLength(150);
        builder.Property(ipAddressLocation => ipAddressLocation.State).HasMaxLength(150);
        builder.Property(ipAddressLocation => ipAddressLocation.Country).HasMaxLength(150);

        builder.HasIndex(ipAddressLocation => ipAddressLocation.IpAddressId);
        builder.HasIndex(ipAddressLocation => ipAddressLocation.ResolvedAtUtc);
        builder.HasIndex(ipAddressLocation => new
        {
            ipAddressLocation.IpAddressId,
            ipAddressLocation.IsCurrentLocation
        })
        .IsUnique()
        .HasFilter("\"IsCurrentLocation\" = TRUE AND \"IsDeleted\" = FALSE");
    }
}
