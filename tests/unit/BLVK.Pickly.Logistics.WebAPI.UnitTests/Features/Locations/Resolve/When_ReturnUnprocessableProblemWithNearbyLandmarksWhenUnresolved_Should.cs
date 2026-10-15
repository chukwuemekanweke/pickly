using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Fact]
    public async Task ReturnUnprocessableProblemWithNearbyLandmarksWhenUnresolved()
    {
        var context = new LocationsControllerTestContext();
        context.ProviderReturns(LocationsControllerTestContext.Candidate("locality", GeocodingPrecision.Area));
        var landmark = new NearbyLocationReadModel(LocationsControllerTestContext.ReadModel(), 300);
        context.ReadModels.FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>())
            .Returns([landmark]);

        var result = await context.CreateController().Resolve(
            LocationsControllerTestContext.ResolveRequest(),
            CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status422UnprocessableEntity, LocationErrorCodes.Unresolved);
        problem.Extensions["nearbyLandmarks"].ShouldBeAssignableTo<IReadOnlyList<NearbyLocationResponse>>()!
            .ShouldHaveSingleItem().Id.ShouldBe(landmark.Location.Id);
    }
}
