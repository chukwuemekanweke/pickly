using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryZonePricingRuleConfiguration : IEntityTypeConfiguration<DeliveryZonePricingRule>
{
    public void Configure(EntityTypeBuilder<DeliveryZonePricingRule> builder)
    {
        builder.ToTable("DeliveryZonePricingRules", SchemaNames.Deliveries, table => table.HasCheckConstraint("CK_DeliveryZonePricingRules_Charge", "\"Charge\" >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Charge).HasPrecision(18, 2).IsRequired();
        builder.HasOne<DeliveryZone>().WithMany().HasForeignKey(x => x.OriginZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryZone>().WithMany().HasForeignKey(x => x.DestinationZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DeliveryPricingPolicyId, x.OriginZoneId, x.DestinationZoneId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
    }
}
