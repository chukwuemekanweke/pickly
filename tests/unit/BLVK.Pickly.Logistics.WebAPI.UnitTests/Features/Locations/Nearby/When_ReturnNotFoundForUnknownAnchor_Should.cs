using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Nearby;

public sealed partial class When_GettingNearbyLocations_WithNearbyRequest_Should
{
[Fact]
    public async Task ReturnNotFoundForUnknownAnchor()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Nearby(
            new GetNearbyLocationsRequest(Guid.CreateVersion7()),
            CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }
}
