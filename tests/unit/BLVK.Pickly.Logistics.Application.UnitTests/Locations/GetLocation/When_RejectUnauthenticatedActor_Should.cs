using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetLocation;

public sealed partial class When_GettingLocation_WithLocationId_Should
{
[Fact]
    public async Task RejectUnauthenticatedActor()
    {
        var result = await new GetLocationHandler(_repository).HandleAsync(
            new GetLocationQuery(Guid.CreateVersion7(), _actor with { StakeholderId = null }),
            CancellationToken.None);

        result.Status.ShouldBe(GetLocationStatus.NotAuthenticated);
    }
}
