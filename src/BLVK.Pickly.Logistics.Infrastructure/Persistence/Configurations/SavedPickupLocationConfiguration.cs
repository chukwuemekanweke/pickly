using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class SavedPickupLocationConfiguration : IEntityTypeConfiguration<SavedPickupLocation>
{
    public void Configure(EntityTypeBuilder<SavedPickupLocation> builder)
    {
        builder.ToTable("SavedPickupLocations", SchemaNames.Deliveries);
        builder.HasKey(location => location.Id);

        builder.Property(location => location.SenderId).IsRequired();
        builder.Property(location => location.LocationId).IsRequired();
        builder.Property(location => location.Label)
            .HasMaxLength(SavedPickupLocation.MaxLabelLength)
            .IsRequired();
        builder.Property(location => location.IsDefault).IsRequired();
        builder.Property(location => location.AdditionalDirections)
            .HasMaxLength(SavedPickupLocation.MaxAdditionalDirectionsLength);

        builder.HasIndex(location => new { location.SenderId, location.CreatedAtUtc, location.Id })
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(location => location.SenderId)
            .IsUnique()
            .HasFilter("\"IsDefault\" = TRUE AND \"IsDeleted\" = FALSE");

        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(location => location.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(location => location.LocationId);
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(location => location.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
