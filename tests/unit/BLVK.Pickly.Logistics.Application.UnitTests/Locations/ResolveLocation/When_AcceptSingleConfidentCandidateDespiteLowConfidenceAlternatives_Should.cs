using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task AcceptSingleConfidentCandidateDespiteLowConfidenceAlternatives()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(
            LocationResolutionTestContext.Candidate(featureId: "address.12", confidence: GeocodingMatchConfidence.High),
            LocationResolutionTestContext.Candidate(featureId: "address.21", addressLine: "21 Ekpulechi Avenue", confidence: GeocodingMatchConfidence.Low));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        context.LocationItems.ShouldHaveSingleItem().HasProviderReference("mapbox", "address.12").ShouldBeTrue();
    }
}
