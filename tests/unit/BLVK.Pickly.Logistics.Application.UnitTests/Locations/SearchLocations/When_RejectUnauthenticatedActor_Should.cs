using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
[Fact]
    public async Task RejectUnauthenticatedActor()
    {
        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("Enugu", null, 10, _actor with { StakeholderId = null }),
            CancellationToken.None);

        result.Status.ShouldBe(SearchLocationsStatus.NotAuthenticated);
    }
}
