using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed partial class When_ManagingSavedPickupLocation_Should
{
[Fact]
    public void PreventDeletedLocationFromBecomingDefault()
    {
        var location = SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            "Home",
            Guid.CreateVersion7(),
            null);
        location.SetDeletedAudit(DateTimeOffset.UtcNow, "test");

        Should.Throw<AggregateStateException>(location.SetAsDefault);
    }
}
