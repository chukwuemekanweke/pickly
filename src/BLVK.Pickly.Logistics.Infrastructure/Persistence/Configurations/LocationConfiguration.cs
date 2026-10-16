using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public const string CoordinatesColumnType = "geography(Point,4326)";
    private const int EnumMaxLength = 50;

    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations", SchemaNames.Locations);
        builder.HasKey(location => location.Id);

        // geography(Point,4326) makes PostGIS enforce the SRID and point type, and measures distances in metres.
        builder.Property(location => location.Coordinates)
            .HasConversion(new GeographicCoordinatesConverter())
            .HasColumnType(CoordinatesColumnType)
            .IsRequired();
        builder.HasIndex(location => location.Coordinates).HasMethod("gist");

        builder.Property(location => location.LocationType)
            .HasConversion<string>()
            .HasMaxLength(EnumMaxLength)
            .IsRequired();
        builder.Property(location => location.ResolutionSource)
            .HasConversion<string>()
            .HasMaxLength(EnumMaxLength)
            .IsRequired();
        builder.Property(location => location.ResolutionStatus)
            .HasConversion<string>()
            .HasMaxLength(EnumMaxLength)
            .IsRequired();
        builder.Property(location => location.Visibility)
            .HasConversion<string>()
            .HasMaxLength(EnumMaxLength)
            .IsRequired();
        builder.Property(location => location.ResolvedAtUtc).IsRequired();
        builder.Ignore(location => location.IsUsable);

        builder.OwnsOne(location => location.Address, owned =>
        {
            owned.Property(address => address.DisplayAddress)
                .HasColumnName("DisplayAddress")
                .HasMaxLength(LocationAddress.MaxDisplayAddressLength)
                .IsRequired();
            owned.Property(address => address.NormalizedAddress)
                .HasColumnName("NormalizedAddress")
                .HasMaxLength(LocationAddress.MaxNormalizedAddressLength)
                .IsRequired();
            owned.Property(address => address.AddressLine)
                .HasColumnName("AddressLine")
                .HasMaxLength(LocationAddress.MaxAddressLineLength)
                .IsRequired();
            owned.Property(address => address.City)
                .HasColumnName("City")
                .HasMaxLength(LocationAddress.MaxLocationNameLength)
                .IsRequired();
            owned.Property(address => address.State)
                .HasColumnName("State")
                .HasMaxLength(LocationAddress.MaxLocationNameLength)
                .IsRequired();
            owned.Property(address => address.CountryCode)
                .HasColumnName("CountryCode")
                .HasMaxLength(LocationAddress.CountryCodeLength)
                .IsFixedLength()
                .IsRequired();
            owned.HasIndex(address => address.NormalizedAddress);
        });
        builder.Navigation(location => location.Address).IsRequired();

        builder.HasMany(location => location.ProviderReferences)
            .WithOne()
            .HasForeignKey(reference => reference.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(location => location.ProviderReferences).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(location => location.ResolutionKeys)
            .WithOne()
            .HasForeignKey(key => key.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(location => location.ResolutionKeys).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
