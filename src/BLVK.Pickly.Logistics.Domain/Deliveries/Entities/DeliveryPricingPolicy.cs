using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryPricingPolicy : Entity, IAggregateRoot
{
    private readonly List<DeliveryVehiclePricingRule> _vehicleRules = [];
    private readonly List<DeliveryZonePricingRule> _zoneRules = [];
    private DeliveryPricingPolicy() { }

    private DeliveryPricingPolicy(string version, Guid currencyId, decimal baseFee, decimal distanceRatePerKilometre, decimal weightRatePerKilogram, DateTimeOffset effectiveFromUtc, DateTimeOffset? effectiveToUtc, DeliveryType? deliveryType, DeliveryOrderType? orderType, bool isActive)
    {
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version.Trim();
        if (Version.Length > 50) throw new ArgumentException("Version must not exceed 50 characters.", nameof(version));
        if (currencyId == Guid.Empty) throw new ArgumentException("Currency is required.", nameof(currencyId));
        ValidateMoney(baseFee, nameof(baseFee));
        ValidateRate(distanceRatePerKilometre, nameof(distanceRatePerKilometre));
        ValidateRate(weightRatePerKilogram, nameof(weightRatePerKilogram));
        CurrencyId = currencyId;
        BaseFee = baseFee;
        DistanceRatePerKilometre = distanceRatePerKilometre;
        WeightRatePerKilogram = weightRatePerKilogram;
        EffectiveFromUtc = effectiveFromUtc.ToUniversalTime();
        EffectiveToUtc = effectiveToUtc?.ToUniversalTime();
        if (EffectiveToUtc <= EffectiveFromUtc) throw new ArgumentException("Policy effective end must be after its start.", nameof(effectiveToUtc));
        DeliveryType = deliveryType;
        OrderType = orderType;
        IsActive = isActive;
    }

    public string Version { get; private set; } = string.Empty;
    public Guid CurrencyId { get; private set; }
    public decimal BaseFee { get; private set; }
    public decimal DistanceRatePerKilometre { get; private set; }
    public decimal WeightRatePerKilogram { get; private set; }
    public DateTimeOffset EffectiveFromUtc { get; private set; }
    public DateTimeOffset? EffectiveToUtc { get; private set; }
    public DeliveryType? DeliveryType { get; private set; }
    public DeliveryOrderType? OrderType { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<DeliveryVehiclePricingRule> VehicleRules => _vehicleRules;
    public IReadOnlyCollection<DeliveryZonePricingRule> ZoneRules => _zoneRules;

    public static DeliveryPricingPolicy Create(string version, Guid currencyId, decimal baseFee, decimal distanceRatePerKilometre, decimal weightRatePerKilogram, DateTimeOffset effectiveFromUtc, DateTimeOffset? effectiveToUtc = null, DeliveryType? deliveryType = null, DeliveryOrderType? orderType = null, bool isActive = true) =>
        new(version, currencyId, baseFee, distanceRatePerKilometre, weightRatePerKilogram, effectiveFromUtc, effectiveToUtc, deliveryType, orderType, isActive);

    public void AddVehicleRule(Guid vehicleCategoryId, decimal charge)
    {
        if (_vehicleRules.Any(x => x.VehicleCategoryId == vehicleCategoryId)) throw new InvalidOperationException("Vehicle pricing is already configured.");
        _vehicleRules.Add(DeliveryVehiclePricingRule.Create(Id, vehicleCategoryId, charge));
    }

    public void AddZoneRule(Guid originZoneId, Guid destinationZoneId, decimal charge)
    {
        if (_zoneRules.Any(x => x.OriginZoneId == originZoneId && x.DestinationZoneId == destinationZoneId)) throw new InvalidOperationException("Zone pricing is already configured.");
        _zoneRules.Add(DeliveryZonePricingRule.Create(Id, originZoneId, destinationZoneId, charge));
    }

    private static void ValidateMoney(decimal value, string parameter)
    {
        if (value < 0m || decimal.Round(value, 2) != value) throw new ArgumentOutOfRangeException(parameter, "Amount must be nonnegative with at most two decimal places.");
    }
    private static void ValidateRate(decimal value, string parameter)
    {
        if (value < 0m || decimal.Round(value, 6) != value) throw new ArgumentOutOfRangeException(parameter, "Rate must be nonnegative with at most six decimal places.");
    }
}
