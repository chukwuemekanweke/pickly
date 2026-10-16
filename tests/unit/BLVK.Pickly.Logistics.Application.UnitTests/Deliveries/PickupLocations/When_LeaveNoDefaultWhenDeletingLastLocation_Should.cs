using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task LeaveNoDefaultWhenDeletingLastLocation()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);

        await context.DeleteHandler().HandleAsync(
            new DeletePickupLocationCommand(created.Location!.Id, context.Actor),
            CancellationToken.None);

        context.Repository.Locations.ShouldBeEmpty();
    }
}
