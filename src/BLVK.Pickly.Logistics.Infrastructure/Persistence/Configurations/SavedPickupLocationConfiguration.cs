using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
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
        builder.Property(location => location.Label)
            .HasMaxLength(SavedPickupLocation.MaxLabelLength)
            .IsRequired();
        builder.Property(location => location.IsDefault).IsRequired();

        builder.HasIndex(location => new { location.SenderId, location.CreatedAtUtc, location.Id })
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(location => location.SenderId)
            .IsUnique()
            .HasFilter("\"IsDefault\" = TRUE AND \"IsDeleted\" = FALSE");

        builder.HasOne<Stakeholder>()
            .WithMany()
            .HasForeignKey(location => location.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(location => location.Address, ConfigureAddress);
    }

    private static void ConfigureAddress(OwnedNavigationBuilder<SavedPickupLocation, DeliveryAddress> owned)
    {
        owned.Property(address => address.AddressLine)
            .HasColumnName("AddressLine")
            .HasMaxLength(300)
            .IsRequired();
        owned.Property(address => address.AdditionalInformation)
            .HasColumnName("AdditionalInformation")
            .HasMaxLength(500);
        owned.Property(address => address.City)
            .HasColumnName("City")
            .HasMaxLength(100)
            .IsRequired();
        owned.Property(address => address.State)
            .HasColumnName("State")
            .HasMaxLength(100)
            .IsRequired();
        owned.Property(address => address.CountryCode)
            .HasColumnName("CountryCode")
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();
        owned.Property(address => address.Latitude)
            .HasColumnName("Latitude")
            .HasPrecision(9, 6)
            .IsRequired();
        owned.Property(address => address.Longitude)
            .HasColumnName("Longitude")
            .HasPrecision(9, 6)
            .IsRequired();
    }
}
