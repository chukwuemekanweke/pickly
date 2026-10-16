using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_EvaluatingVehicleCategory_Should
{
    [Fact]
    public void AcceptWeightAtCapacity() => VehicleCategory.Create("VAN", "Van", null, 100m, "driving").CanCarry(100m).ShouldBeTrue();

    [Fact]
    public void RejectWeightOverCapacity() => VehicleCategory.Create("BIKE", "Bike", null, 20m, "driving").CanCarry(20.001m).ShouldBeFalse();

    [Fact]
    public void RejectInactiveCategory() => VehicleCategory.Create("CAR", "Car", null, 100m, "driving", false).CanCarry(1m).ShouldBeFalse();
}
