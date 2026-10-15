using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Fact]
    public void RejectEmptySenderIdentifier()
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.Empty,
            "Home",
            Guid.CreateVersion7(),
            null));
    }
}
