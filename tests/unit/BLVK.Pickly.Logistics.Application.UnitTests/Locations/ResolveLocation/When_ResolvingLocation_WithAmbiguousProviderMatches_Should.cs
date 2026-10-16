using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithAmbiguousProviderMatches_Should
{
    [Fact]
    public async Task RequireExplicitSelectionWithoutPersistingAnyCandidate()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(
            LocationResolutionTestContext.Candidate(featureId: "address.12a", confidence: GeocodingMatchConfidence.High),
            LocationResolutionTestContext.Candidate(featureId: "address.12b", addressLine: "12 Ekpulechi Close", confidence: GeocodingMatchConfidence.High));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.SelectionRequired);
        result.Location.ShouldBeNull();
        result.Candidates!.Count.ShouldBe(2);
        result.Candidates.Select(candidate => candidate.SelectionToken).Distinct().Count().ShouldBe(2);
        context.LocationItems.ShouldBeEmpty();
    }
}
