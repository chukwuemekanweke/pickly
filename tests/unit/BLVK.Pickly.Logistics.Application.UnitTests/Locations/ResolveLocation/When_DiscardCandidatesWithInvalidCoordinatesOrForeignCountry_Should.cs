using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConfidentProviderMatch_Should
{
[Fact]
    public async Task DiscardCandidatesWithInvalidCoordinatesOrForeignCountry()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(
            LocationResolutionTestContext.Candidate(featureId: "address.bad", latitude: 95m),
            LocationResolutionTestContext.Candidate(featureId: "address.gh", countryCode: "GH"),
            LocationResolutionTestContext.Candidate(featureId: "address.12"));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        context.LocationItems.ShouldHaveSingleItem().HasProviderReference("mapbox", "address.12").ShouldBeTrue();
    }
}
