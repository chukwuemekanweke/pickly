using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Fact]
    public async Task ReturnCandidatesRequiringSelection()
    {
        var context = new LocationsControllerTestContext();
        context.ProviderReturns(
            LocationsControllerTestContext.Candidate("a", confidence: GeocodingMatchConfidence.High),
            LocationsControllerTestContext.Candidate("b", confidence: GeocodingMatchConfidence.High));

        var result = await context.CreateController().Resolve(
            LocationsControllerTestContext.ResolveRequest(),
            CancellationToken.None);

        var response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ResolveLocationResponse>();
        response.Status.ShouldBe(ResolveLocationResponse.SelectionRequiredStatus);
        response.LocationId.ShouldBeNull();
        response.Candidates.Count.ShouldBe(2);
        response.Candidates.ShouldAllBe(candidate => !string.IsNullOrWhiteSpace(candidate.SelectionToken));
    }
}
