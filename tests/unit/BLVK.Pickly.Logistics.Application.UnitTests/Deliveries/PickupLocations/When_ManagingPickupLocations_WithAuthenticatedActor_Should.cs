using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
    [Fact]
    public async Task MakeFirstLocationDefaultAndLeaveAdditionalLocationNonDefault()
    {
        var context = new PickupLocationHandlerTestContext();
        var handler = context.CreateHandler();

        var first = await handler.HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);
        var second = await handler.HandleAsync(
            context.CreateCommand("Office", false),
            CancellationToken.None);

        first.Status.ShouldBe(CreatePickupLocationStatus.Success);
        first.Location!.IsDefault.ShouldBeTrue();
        second.Location!.IsDefault.ShouldBeFalse();
        context.Repository.Locations.Count(location => location.IsDefault).ShouldBe(1);
    }
}
