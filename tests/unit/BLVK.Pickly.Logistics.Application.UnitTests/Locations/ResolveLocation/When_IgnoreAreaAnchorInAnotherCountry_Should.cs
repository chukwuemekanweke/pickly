using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_EvaluatingGeocodingCandidates_Should
{
[Fact]
    public void IgnoreAreaAnchorInAnotherCountry()
    {
        var evaluation = GeocodingCandidateEvaluation.Evaluate(
            [LocationResolutionTestContext.Candidate(precision: GeocodingPrecision.Area, countryCode: "GH")],
            "NG");

        evaluation.ApproximateAnchor.ShouldBeNull();
    }
}
