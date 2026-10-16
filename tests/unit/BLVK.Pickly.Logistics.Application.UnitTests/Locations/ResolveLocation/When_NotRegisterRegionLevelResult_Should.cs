using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithoutPreciseProviderMatch_Should
{
[Fact]
    public async Task NotRegisterRegionLevelResult()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate(
            featureId: "region.enugu",
            addressLine: "Enugu",
            precision: GeocodingPrecision.Region));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Unresolved);
        context.LocationItems.ShouldBeEmpty();
    }
}
