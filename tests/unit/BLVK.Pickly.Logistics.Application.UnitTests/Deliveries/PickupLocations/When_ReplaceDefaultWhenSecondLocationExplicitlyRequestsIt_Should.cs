using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task ReplaceDefaultWhenSecondLocationExplicitlyRequestsIt()
    {
        var context = new PickupLocationHandlerTestContext();
        var handler = context.CreateHandler();

        var first = await handler.HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);
        var second = await handler.HandleAsync(
            context.CreateCommand("Office", true),
            CancellationToken.None);

        context.Repository.Locations.Single(location => location.Id == first.Location!.Id).IsDefault.ShouldBeFalse();
        context.Repository.Locations.Single(location => location.Id == second.Location!.Id).IsDefault.ShouldBeTrue();
    }
}
