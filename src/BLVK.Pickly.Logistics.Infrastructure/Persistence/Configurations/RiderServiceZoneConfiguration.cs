using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class RiderServiceZoneConfiguration : IEntityTypeConfiguration<RiderServiceZone>
{
    public void Configure(EntityTypeBuilder<RiderServiceZone> builder)
    {
        builder.ToTable("RiderServiceZones", SchemaNames.Riders);
        builder.HasKey(serviceZone => serviceZone.Id);
        builder.Property(serviceZone => serviceZone.RiderProfileId).IsRequired();
        builder.Property(serviceZone => serviceZone.DeliveryZoneId).IsRequired();
        builder.HasIndex(serviceZone => new { serviceZone.RiderProfileId, serviceZone.DeliveryZoneId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(serviceZone => serviceZone.DeliveryZoneId)
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasOne<RiderProfile>()
            .WithMany()
            .HasForeignKey(serviceZone => serviceZone.RiderProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DeliveryZone>()
            .WithMany()
            .HasForeignKey(serviceZone => serviceZone.DeliveryZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
