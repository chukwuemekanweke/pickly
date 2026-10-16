using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Fact]
    public async Task RejectRetiredLocationOnCreate()
    {
        var context = new PickupLocationHandlerTestContext();
        context.HomeLocation.Retire();

        var result = await context.CreateHandler().HandleAsync(context.CreateCommand("Home"), CancellationToken.None);

        result.Status.ShouldBe(CreatePickupLocationStatus.LocationUnusable);
        context.Repository.Locations.ShouldBeEmpty();
    }
}
