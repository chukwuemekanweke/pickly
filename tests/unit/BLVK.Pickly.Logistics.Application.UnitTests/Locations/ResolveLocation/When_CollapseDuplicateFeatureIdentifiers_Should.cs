using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_EvaluatingGeocodingCandidates_Should
{
[Fact]
    public void CollapseDuplicateFeatureIdentifiers()
    {
        var evaluation = GeocodingCandidateEvaluation.Evaluate(
            [LocationResolutionTestContext.Candidate(featureId: "a"), LocationResolutionTestContext.Candidate(featureId: "a")],
            "NG");

        evaluation.AcceptedCandidate.ShouldNotBeNull();
    }
}
