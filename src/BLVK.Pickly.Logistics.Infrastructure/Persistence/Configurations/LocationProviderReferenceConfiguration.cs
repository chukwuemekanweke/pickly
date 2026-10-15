using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class LocationProviderReferenceConfiguration : IEntityTypeConfiguration<LocationProviderReference>
{
    public void Configure(EntityTypeBuilder<LocationProviderReference> builder)
    {
        builder.ToTable("LocationProviderReferences", SchemaNames.Locations);
        builder.HasKey(reference => reference.Id);
        builder.Property(reference => reference.Provider)
            .HasMaxLength(LocationProviderReference.MaxProviderLength)
            .IsRequired();
        builder.Property(reference => reference.FeatureId)
            .HasMaxLength(LocationProviderReference.MaxFeatureIdLength)
            .IsRequired();
        builder.Property(reference => reference.FeatureType)
            .HasMaxLength(LocationProviderReference.MaxFeatureTypeLength)
            .IsRequired();
        builder.Property(reference => reference.StorageClassification)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(reference => reference.ResolvedAtUtc).IsRequired();

        // The same provider feature must never produce two shared locations.
        builder.HasIndex(reference => new { reference.Provider, reference.FeatureId }).IsUnique();
    }
}
