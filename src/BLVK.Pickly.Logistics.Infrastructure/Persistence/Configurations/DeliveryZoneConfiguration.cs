using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public const string BoundaryProperty = "Boundary";
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("DeliveryZones", SchemaNames.Deliveries, table => table.HasCheckConstraint("CK_DeliveryZones_EffectivePeriod", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\""));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property<MultiPolygon>(BoundaryProperty).HasColumnType("geometry(MultiPolygon,4326)").IsRequired();
        builder.HasIndex(BoundaryProperty).HasMethod("gist");
        builder.HasIndex(x => new { x.CountryCode, x.Code }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => new { x.IsActive, x.EffectiveFromUtc, x.EffectiveToUtc }).HasFilter("\"IsDeleted\" = FALSE");
    }
}
