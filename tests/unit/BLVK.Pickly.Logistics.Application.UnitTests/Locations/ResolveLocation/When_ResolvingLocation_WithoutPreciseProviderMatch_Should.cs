using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithoutPreciseProviderMatch_Should
{
    [Fact]
    public async Task ReturnUnresolvedWithoutFabricatingCoordinatesWhenProviderHasNoMatch()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns();

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Unresolved);
        result.Location.ShouldBeNull();
        result.NearbyLandmarks.ShouldBeEmpty();
        context.LocationItems.ShouldBeEmpty();
        await context.ReadModels.DidNotReceive().FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>());
    }
}
