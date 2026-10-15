using BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetNearbyLocations;

public sealed partial class When_GettingNearbyLocations_WithAnchorLocation_Should
{
[Fact]
    public async Task ReturnAnchorNotFoundForUnknownLocation()
    {
        var result = await Handler().HandleAsync(
            new GetNearbyLocationsQuery(Guid.CreateVersion7(), 1000, 10, null, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(GetNearbyLocationsStatus.AnchorNotFound);
        await _repository.DidNotReceive().FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>());
    }
}
