using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
    [Fact]
    public void NormalizeValidLocationAssociation()
    {
        var sharedLocationId = Guid.CreateVersion7();
        var location = SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            "  Home  ",
            sharedLocationId,
            "  Near the gate  ");

        location.Label.ShouldBe("Home");
        location.LocationId.ShouldBe(sharedLocationId);
        location.AdditionalDirections.ShouldBe("Near the gate");
        location.IsDefault.ShouldBeFalse();
    }
}
