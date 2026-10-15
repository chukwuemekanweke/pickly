using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithAmbiguousProviderMatches_Should
{
[Fact]
    public async Task ReuseCachedProviderOutcomeForRepeatedAmbiguousRequest()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(
            LocationResolutionTestContext.Candidate(featureId: "address.12a", confidence: GeocodingMatchConfidence.High),
            LocationResolutionTestContext.Candidate(featureId: "address.12b", confidence: GeocodingMatchConfidence.High));
        var handler = context.ResolveHandler();

        await handler.HandleAsync(context.Command(), CancellationToken.None);
        var repeated = await handler.HandleAsync(context.Command(), CancellationToken.None);

        repeated.Status.ShouldBe(ResolveLocationStatus.SelectionRequired);
        await context.ProviderCalls(1);
    }
}
