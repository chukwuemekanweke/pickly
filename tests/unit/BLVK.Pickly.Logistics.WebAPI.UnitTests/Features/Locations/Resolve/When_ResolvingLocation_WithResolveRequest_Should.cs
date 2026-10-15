using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
    [Fact]
    public async Task ReturnResolvedLocationContract()
    {
        var context = new LocationsControllerTestContext();
        context.ProviderReturns(LocationsControllerTestContext.Candidate());

        var result = await context.CreateController().Resolve(
            LocationsControllerTestContext.ResolveRequest(),
            CancellationToken.None);

        var response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<ResolveLocationResponse>();
        response.Status.ShouldBe(ResolveLocationResponse.ResolvedStatus);
        response.LocationId.ShouldNotBeNull();
        response.DisplayAddress.ShouldBe("12 Ekpulechi Avenue, Thinkers Corner, Enugu");
        response.LocationType.ShouldBe(LocationType.Address);
        response.ResolutionSource.ShouldBe(LocationResolutionSource.Mapbox);
        response.Coordinates.ShouldBe(new LocationCoordinatesResponse(6.4512m, 7.5221m));
        response.Candidates.ShouldBeEmpty();
    }

    private static ProblemDetails AssertProblem(ActionResult<ResolveLocationResponse> result, int statusCode, string code)
    {
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(statusCode);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Extensions["code"].ShouldBe(code);
        return problem;
    }
}
