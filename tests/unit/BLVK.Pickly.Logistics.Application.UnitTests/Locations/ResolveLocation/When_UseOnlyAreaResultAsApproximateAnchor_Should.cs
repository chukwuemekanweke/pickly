using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_EvaluatingGeocodingCandidates_Should
{
[Fact]
    public void UseOnlyAreaResultAsApproximateAnchor()
    {
        var evaluation = GeocodingCandidateEvaluation.Evaluate(
            [LocationResolutionTestContext.Candidate(featureId: "locality", precision: GeocodingPrecision.Area)],
            "NG");

        evaluation.AcceptedCandidate.ShouldBeNull();
        evaluation.SelectableCandidates.ShouldBeEmpty();
        evaluation.ApproximateAnchor.ShouldNotBeNull();
    }
}
