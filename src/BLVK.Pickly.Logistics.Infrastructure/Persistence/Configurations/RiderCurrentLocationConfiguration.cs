using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class RiderCurrentLocationConfiguration : IEntityTypeConfiguration<RiderCurrentLocation>
{
    public const string PositionProperty = "Position";

    public void Configure(EntityTypeBuilder<RiderCurrentLocation> builder)
    {
        builder.ToTable("RiderCurrentLocations", SchemaNames.Riders);
        builder.HasKey(location => location.Id);
        builder.Property(location => location.RiderProfileId).IsRequired();
        builder.Property(location => location.CapturedAtUtc).IsRequired();
        builder.Property(location => location.ReceivedAtUtc).IsRequired();
        builder.Property<Point>(PositionProperty).HasColumnType("geography(Point,4326)").IsRequired();

        builder.HasIndex(location => location.RiderProfileId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(PositionProperty).HasMethod("gist");
        builder.HasIndex(location => location.CapturedAtUtc)
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<RiderProfile>()
            .WithOne()
            .HasForeignKey<RiderCurrentLocation>(location => location.RiderProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
