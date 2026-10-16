using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryVehiclePricingRuleConfiguration : IEntityTypeConfiguration<DeliveryVehiclePricingRule>
{
    public void Configure(EntityTypeBuilder<DeliveryVehiclePricingRule> builder)
    {
        builder.ToTable("DeliveryVehiclePricingRules", SchemaNames.Deliveries, table => table.HasCheckConstraint("CK_DeliveryVehiclePricingRules_Charge", "\"Charge\" >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Charge).HasPrecision(18, 2).IsRequired();
        builder.HasOne<VehicleCategory>().WithMany().HasForeignKey(x => x.VehicleCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DeliveryPricingPolicyId, x.VehicleCategoryId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
    }
}
