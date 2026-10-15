using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Nearby;

public sealed partial class When_GettingNearbyLocations_WithNearbyRequest_Should
{
[Theory]
    [InlineData(0, 10)]
    [InlineData(20000, 10)]
    [InlineData(1000, 0)]
    [InlineData(1000, 100)]
    public async Task ReturnBadRequestForUnboundedQuery(int radiusMeters, int limit)
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Nearby(
            new GetNearbyLocationsRequest(Guid.CreateVersion7(), radiusMeters, limit),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        await context.ReadModels.DidNotReceive().FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>());
    }
}
