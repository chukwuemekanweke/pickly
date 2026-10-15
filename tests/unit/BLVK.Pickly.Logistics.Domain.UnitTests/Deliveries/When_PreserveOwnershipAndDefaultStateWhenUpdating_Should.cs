using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Fact]
    public void PreserveOwnershipAndDefaultStateWhenUpdating()
    {
        var senderId = Guid.CreateVersion7();
        var originalLocationId = Guid.CreateVersion7();
        var updatedLocationId = Guid.CreateVersion7();
        var location = SavedPickupLocation.Create(senderId, "Home", originalLocationId, "Gate A", true);

        location.Update(" Office ", updatedLocationId, "Gate B");

        location.SenderId.ShouldBe(senderId);
        location.Label.ShouldBe("Office");
        location.LocationId.ShouldBe(updatedLocationId);
        location.AdditionalDirections.ShouldBe("Gate B");
        location.IsDefault.ShouldBeTrue();
    }
}
