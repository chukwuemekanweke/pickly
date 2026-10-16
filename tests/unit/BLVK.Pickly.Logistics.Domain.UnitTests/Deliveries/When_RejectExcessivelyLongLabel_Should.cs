using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Fact]
    public void RejectExcessivelyLongLabel()
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            new string('x', SavedPickupLocation.MaxLabelLength + 1),
            Guid.CreateVersion7(),
            null));
    }
}
