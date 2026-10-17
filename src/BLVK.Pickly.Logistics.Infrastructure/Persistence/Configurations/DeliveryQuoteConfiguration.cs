using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;

public sealed class DeliveryQuoteConfiguration : IEntityTypeConfiguration<DeliveryQuote>
{
    public void Configure(EntityTypeBuilder<DeliveryQuote> builder)
    {
        builder.ToTable("DeliveryQuotes", SchemaNames.Deliveries, table =>
        {
            table.HasCheckConstraint("CK_DeliveryQuotes_Expiry", "\"ExpiresAtUtc\" > \"IssuedAtUtc\"");
            table.HasCheckConstraint("CK_DeliveryQuotes_Total", "\"TotalAmount\" >= 0");
            table.HasCheckConstraint(
                "CK_DeliveryQuotes_Consumption",
                "(\"ConsumptionStatus\" = 1 AND \"ConsumedByDeliveryId\" IS NULL AND \"ConsumedAtUtc\" IS NULL) OR " +
                "(\"ConsumptionStatus\" = 2 AND \"ConsumedByDeliveryId\" IS NOT NULL AND \"ConsumedAtUtc\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeliveryType).HasConversion<int>().IsRequired();
        builder.Property(x => x.OrderType).HasConversion<int>().IsRequired();
        builder.Property(x => x.ConsumptionStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.VehicleCategoryCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.VehicleCategoryName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OriginZoneCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DestinationZoneCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PricingPolicyVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TotalWeightKg).HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.OwnsOne(x => x.PickupAddress, owned => ConfigureAddress(owned, "Pickup"));
        builder.OwnsOne(x => x.DropOffAddress, owned => ConfigureAddress(owned, "DropOff"));
        builder.OwnsOne(x => x.Pricing, owned => { owned.Property(x => x.BaseFee).HasColumnName("BaseFee").HasPrecision(18, 2); owned.Property(x => x.DistanceCharge).HasColumnName("DistanceCharge").HasPrecision(18, 2); owned.Property(x => x.WeightCharge).HasColumnName("WeightCharge").HasPrecision(18, 2); owned.Property(x => x.VehicleCharge).HasColumnName("VehicleCharge").HasPrecision(18, 2); owned.Property(x => x.ZoneCharge).HasColumnName("ZoneCharge").HasPrecision(18, 2); owned.Ignore(x => x.TotalAmount); });
        builder.HasOne<Stakeholder>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleCategory>().WithMany().HasForeignKey(x => x.VehicleCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryZone>().WithMany().HasForeignKey(x => x.OriginZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryZone>().WithMany().HasForeignKey(x => x.DestinationZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryPricingPolicy>().WithMany().HasForeignKey(x => x.PricingPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.DeliveryQuoteId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.SenderId, x.CreatedAtUtc }).HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.ConsumedByDeliveryId).IsUnique().HasFilter("\"ConsumedByDeliveryId\" IS NOT NULL");
        builder.HasOne<Delivery>()
            .WithMany()
            .HasForeignKey(nameof(DeliveryQuote.ConsumedByDeliveryId), nameof(DeliveryQuote.Id))
            .HasPrincipalKey(nameof(Delivery.Id), nameof(Delivery.DeliveryQuoteId))
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAddress(OwnedNavigationBuilder<DeliveryQuote, DeliveryAddress> owned, string prefix)
    {
        owned.Property(x => x.AddressLine).HasColumnName($"{prefix}AddressLine").HasMaxLength(300).IsRequired();
        owned.Property(x => x.AdditionalInformation).HasColumnName($"{prefix}AdditionalInformation").HasMaxLength(500);
        owned.Property(x => x.City).HasColumnName($"{prefix}City").HasMaxLength(100).IsRequired();
        owned.Property(x => x.State).HasColumnName($"{prefix}State").HasMaxLength(100).IsRequired();
        owned.Property(x => x.CountryCode).HasColumnName($"{prefix}CountryCode").HasMaxLength(2).IsFixedLength().IsRequired();
        owned.Property(x => x.Latitude).HasColumnName($"{prefix}Latitude").HasPrecision(9, 6).IsRequired();
        owned.Property(x => x.Longitude).HasColumnName($"{prefix}Longitude").HasPrecision(9, 6).IsRequired();
    }
}
