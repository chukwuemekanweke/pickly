using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_CalculatingConfiguredDeliveryPrice_Should
{
    [Fact]
    public void IncludeAllFiveComponents_WithDeterministicRounding()
    {
        var vehicleId = Guid.CreateVersion7();
        var originZoneId = Guid.CreateVersion7();
        var destinationZoneId = Guid.CreateVersion7();
        var policy = DeliveryPricingPolicy.Create("test-v1", Guid.CreateVersion7(), 100m, 25.125m, 10.50m, DateTimeOffset.UtcNow.AddMinutes(-1));
        policy.AddVehicleRule(vehicleId, 30m);
        policy.AddZoneRule(originZoneId, destinationZoneId, 40m);

        var result = DeliveryPricingCalculator.Calculate(policy, vehicleId, originZoneId, destinationZoneId, 1500, 3.250m);

        result.BaseFee.ShouldBe(100m);
        result.DistanceCharge.ShouldBe(37.69m);
        result.WeightCharge.ShouldBe(34.13m);
        result.VehicleCharge.ShouldBe(30m);
        result.ZoneCharge.ShouldBe(40m);
        result.TotalAmount.ShouldBe(241.82m);
    }

    [Fact]
    public void AccountForParcelQuantities()
    {
        DeliveryPricingCalculator.CalculateTotalWeight([(1, 50m), (2, 12m)]).ShouldBe(74m);
    }

    [Fact]
    public void RejectMissingDirectionalZoneRule()
    {
        var vehicleId = Guid.CreateVersion7();
        var policy = DeliveryPricingPolicy.Create("test-v1", Guid.CreateVersion7(), 0m, 0m, 0m, DateTimeOffset.UtcNow.AddMinutes(-1));
        policy.AddVehicleRule(vehicleId, 0m);
        policy.AddZoneRule(Guid.CreateVersion7(), Guid.CreateVersion7(), 0m);

        Should.Throw<InvalidOperationException>(() => DeliveryPricingCalculator.Calculate(policy, vehicleId, Guid.CreateVersion7(), Guid.CreateVersion7(), 1, 1m));
    }
}
