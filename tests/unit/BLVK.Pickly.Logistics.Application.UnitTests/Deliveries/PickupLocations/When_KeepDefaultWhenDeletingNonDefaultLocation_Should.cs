using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task KeepDefaultWhenDeletingNonDefaultLocation()
    {
        var context = new PickupLocationHandlerTestContext();
        var create = context.CreateHandler();
        var first = await create.HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);
        var second = await create.HandleAsync(
            context.CreateCommand("Office", false),
            CancellationToken.None);

        var result = await context.DeleteHandler().HandleAsync(
            new DeletePickupLocationCommand(second.Location!.Id, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(DeletePickupLocationStatus.Success);
        context.Repository.Locations.Single().Id.ShouldBe(first.Location!.Id);
        context.Repository.Locations.Single().IsDefault.ShouldBeTrue();
    }
}
