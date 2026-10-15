using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task SelectOldestReplacementWhenDeletingDefault()
    {
        var context = new PickupLocationHandlerTestContext();
        var create = context.CreateHandler();
        var first = await create.HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);
        var oldestRemaining = await create.HandleAsync(
            context.CreateCommand("Office", false),
            CancellationToken.None);
        await create.HandleAsync(
            context.CreateCommand("Warehouse", false),
            CancellationToken.None);

        await context.DeleteHandler().HandleAsync(
            new DeletePickupLocationCommand(first.Location!.Id, context.Actor),
            CancellationToken.None);

        context.Repository.Locations.Single(location => location.IsDefault).Id.ShouldBe(oldestRemaining.Location!.Id);
    }
}
