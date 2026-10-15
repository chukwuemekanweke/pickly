using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionToken_Should
{
    [Fact]
    public async Task RegisterExplicitlyChosenCandidate()
    {
        var context = new LocationResolutionTestContext();
        var candidates = await ResolveAmbiguouslyAsync(context);

        var result = await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand(candidates[1].SelectionToken, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(SelectLocationCandidateStatus.Resolved);
        var registered = context.LocationItems.ShouldHaveSingleItem();
        registered.HasProviderReference("mapbox", "address.12b").ShouldBeTrue();
        result.Location!.LocationId.ShouldBe(registered.Id);
    }

    private static async Task<IReadOnlyList<LocationCandidateResponse>> ResolveAmbiguouslyAsync(
        LocationResolutionTestContext context)
    {
        context.ProviderReturns(
            LocationResolutionTestContext.Candidate(featureId: "address.12a", confidence: GeocodingMatchConfidence.Medium),
            LocationResolutionTestContext.Candidate(
                featureId: "address.12b",
                addressLine: "12 Ekpulechi Close",
                confidence: GeocodingMatchConfidence.Medium));
        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);
        result.Status.ShouldBe(ResolveLocationStatus.SelectionRequired);
        return result.Candidates!;
    }
}
