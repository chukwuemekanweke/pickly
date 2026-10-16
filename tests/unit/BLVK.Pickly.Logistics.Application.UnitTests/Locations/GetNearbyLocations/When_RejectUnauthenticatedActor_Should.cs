using BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetNearbyLocations;

public sealed partial class When_GettingNearbyLocations_WithAnchorLocation_Should
{
[Fact]
    public async Task RejectUnauthenticatedActor()
    {
        var result = await Handler().HandleAsync(
            new GetNearbyLocationsQuery(Guid.CreateVersion7(), 1000, 10, null, _actor with { StakeholderId = null }),
            CancellationToken.None);

        result.Status.ShouldBe(GetNearbyLocationsStatus.NotAuthenticated);
    }
}
