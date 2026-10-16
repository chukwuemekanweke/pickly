using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryPricingPolicyConfiguration : IEntityTypeConfiguration<DeliveryPricingPolicy>
{
    public void Configure(EntityTypeBuilder<DeliveryPricingPolicy> builder)
    {
        builder.ToTable("DeliveryPricingPolicies", SchemaNames.Deliveries, table =>
        {
            table.HasCheckConstraint("CK_DeliveryPricingPolicies_EffectivePeriod", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
            table.HasCheckConstraint("CK_DeliveryPricingPolicies_NonnegativeRates", "\"BaseFee\" >= 0 AND \"DistanceRatePerKilometre\" >= 0 AND \"WeightRatePerKilogram\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BaseFee).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DistanceRatePerKilometre).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.WeightRatePerKilogram).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.DeliveryType).HasConversion<int?>();
        builder.Property(x => x.OrderType).HasConversion<int?>();
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Version).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasMany(x => x.VehicleRules).WithOne().HasForeignKey(x => x.DeliveryPricingPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ZoneRules).WithOne().HasForeignKey(x => x.DeliveryPricingPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.VehicleRules).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ZoneRules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
