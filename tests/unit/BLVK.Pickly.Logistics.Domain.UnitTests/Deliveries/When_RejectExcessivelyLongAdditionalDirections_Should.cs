using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Fact]
    public void RejectExcessivelyLongAdditionalDirections()
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            "Home",
            Guid.CreateVersion7(),
            new string('x', SavedPickupLocation.MaxAdditionalDirectionsLength + 1)));
    }
}
