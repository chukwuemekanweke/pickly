using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class LocationResolutionKeyConfiguration : IEntityTypeConfiguration<LocationResolutionKey>
{
    public void Configure(EntityTypeBuilder<LocationResolutionKey> builder)
    {
        builder.ToTable("LocationResolutionKeys", SchemaNames.Locations);
        builder.HasKey(key => key.Id);
        builder.Property(key => key.Key)
            .HasMaxLength(LocationAddress.ResolutionKeyLength)
            .IsFixedLength()
            .IsRequired();
        builder.HasIndex(key => key.Key).IsUnique();
        builder.HasIndex(key => key.LocationId);
    }
}
