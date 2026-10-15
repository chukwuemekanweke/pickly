using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithAmbiguousProviderMatches_Should
{
[Fact]
    public async Task StoreCandidatesServerSideBoundToTheSender()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(
            LocationResolutionTestContext.Candidate(featureId: "address.12a", confidence: GeocodingMatchConfidence.Medium),
            LocationResolutionTestContext.Candidate(featureId: "address.12b", confidence: GeocodingMatchConfidence.Medium));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        LocationSelectionTokens.TryParse(result.Candidates![0].SelectionToken, out var selectionId, out _).ShouldBeTrue();
        var pending = await context.Cache.GetAsync<PendingLocationSelection>(
            LocationSelectionTokens.SelectionCacheKey(selectionId),
            CancellationToken.None);
        pending.ShouldNotBeNull();
        pending.StakeholderId.ShouldBe(context.StakeholderId);
        pending.Candidates.Select(candidate => candidate.FeatureId).ShouldBe(["address.12a", "address.12b"]);
    }
}
