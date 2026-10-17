using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Services;

public static class DeliveryPricingCalculator
{
    public static DeliveryPricingBreakdown Calculate(DeliveryPricingPolicy policy, Guid vehicleCategoryId, Guid originZoneId, Guid destinationZoneId, long distanceMeters, decimal totalWeightKg)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (distanceMeters < 0) throw new ArgumentOutOfRangeException(nameof(distanceMeters));
        if (totalWeightKg <= 0m) throw new ArgumentOutOfRangeException(nameof(totalWeightKg));
        var vehicleRule = policy.VehicleRules.SingleOrDefault(x => x.VehicleCategoryId == vehicleCategoryId)
            ?? throw new InvalidOperationException("Vehicle pricing is not configured by the selected policy.");
        var zoneRule = policy.ZoneRules.SingleOrDefault(x => x.OriginZoneId == originZoneId && x.DestinationZoneId == destinationZoneId)
            ?? throw new InvalidOperationException("Zone-pair pricing is not configured by the selected policy.");
        return new DeliveryPricingBreakdown(
            policy.BaseFee,
            Money(checked(distanceMeters / 1000m * policy.DistanceRatePerKilometre)),
            Money(checked(totalWeightKg * policy.WeightRatePerKilogram)),
            vehicleRule.Charge,
            zoneRule.Charge);
    }

    public static decimal CalculateTotalWeight(IEnumerable<(int Quantity, decimal WeightKg)> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        decimal total = 0m;
        var any = false;
        foreach (var item in items)
        {
            any = true;
            if (item.Quantity <= 0 || item.WeightKg <= 0m || decimal.Round(item.WeightKg, 3) != item.WeightKg) throw new ArgumentOutOfRangeException(nameof(items), "Parcel quantity and weight must be positive; weight supports three decimal places.");
            total = checked(total + checked(item.Quantity * item.WeightKg));
        }
        if (!any) throw new ArgumentException("At least one parcel is required.", nameof(items));
        return total;
    }

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
