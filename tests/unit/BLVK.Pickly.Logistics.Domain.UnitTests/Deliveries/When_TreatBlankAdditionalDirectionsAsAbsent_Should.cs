using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Fact]
    public void TreatBlankAdditionalDirectionsAsAbsent()
    {
        var location = SavedPickupLocation.Create(Guid.CreateVersion7(), "Home", Guid.CreateVersion7(), "   ");

        location.AdditionalDirections.ShouldBeNull();
    }
}
