using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class LocationResolutionLeaseConfiguration : IEntityTypeConfiguration<LocationResolutionLeaseRecord>
{
    public void Configure(EntityTypeBuilder<LocationResolutionLeaseRecord> builder)
    {
        builder.ToTable("LocationResolutionLeases", SchemaNames.Locations);
        builder.HasKey(lease => lease.ResolutionKey);
        builder.Property(lease => lease.ResolutionKey)
            .HasMaxLength(LocationAddress.ResolutionKeyLength)
            .IsFixedLength();
        builder.Property(lease => lease.LeaseId).IsRequired();
        builder.Property(lease => lease.ExpiresAtUtc).IsRequired();
    }
}
