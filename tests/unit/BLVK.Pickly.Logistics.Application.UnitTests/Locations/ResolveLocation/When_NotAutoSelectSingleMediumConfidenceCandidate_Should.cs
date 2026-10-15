using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithAmbiguousProviderMatches_Should
{
[Fact]
    public async Task NotAutoSelectSingleMediumConfidenceCandidate()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate(confidence: GeocodingMatchConfidence.Medium));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.SelectionRequired);
        context.LocationItems.ShouldBeEmpty();
    }
}
