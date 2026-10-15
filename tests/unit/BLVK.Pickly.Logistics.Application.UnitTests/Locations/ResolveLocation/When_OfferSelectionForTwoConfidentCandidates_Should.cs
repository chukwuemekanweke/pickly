using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_EvaluatingGeocodingCandidates_Should
{
[Fact]
    public void OfferSelectionForTwoConfidentCandidates()
    {
        var evaluation = GeocodingCandidateEvaluation.Evaluate(
            [
                LocationResolutionTestContext.Candidate(featureId: "a"),
                LocationResolutionTestContext.Candidate(featureId: "b", confidence: GeocodingMatchConfidence.High)
            ],
            "NG");

        evaluation.AcceptedCandidate.ShouldBeNull();
        evaluation.SelectableCandidates.Count.ShouldBe(2);
    }
}
