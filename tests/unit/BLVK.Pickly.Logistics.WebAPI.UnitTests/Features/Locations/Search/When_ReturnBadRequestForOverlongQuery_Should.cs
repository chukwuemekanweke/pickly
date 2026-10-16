using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Search;

public sealed partial class When_SearchingLocations_WithSearchRequest_Should
{
[Fact]
    public async Task ReturnBadRequestForOverlongQuery()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Search(
            new SearchLocationsRequest(new string('x', 101)),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
