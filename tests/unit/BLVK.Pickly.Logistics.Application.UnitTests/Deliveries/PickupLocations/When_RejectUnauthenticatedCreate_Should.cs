using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task RejectUnauthenticatedCreate()
    {
        var context = new PickupLocationHandlerTestContext();

        var result = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand(
                context.HomeLocation.Id,
                "Home",
                null,
                false,
                new ActorContext(null, null, "correlation", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(CreatePickupLocationStatus.NotAuthenticated);
        context.Repository.Locations.ShouldBeEmpty();
    }
}
