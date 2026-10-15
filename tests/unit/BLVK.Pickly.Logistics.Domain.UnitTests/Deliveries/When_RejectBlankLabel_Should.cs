using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectBlankLabel(string label)
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            label,
            Guid.CreateVersion7(),
            null));
    }
}
