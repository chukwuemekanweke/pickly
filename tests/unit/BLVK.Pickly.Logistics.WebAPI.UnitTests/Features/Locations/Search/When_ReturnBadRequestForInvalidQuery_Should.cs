using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Search;

public sealed partial class When_SearchingLocations_WithSearchRequest_Should
{
[Theory]
    [InlineData(null, 10)]
    [InlineData("a", 10)]
    [InlineData("Polo Park", 0)]
    [InlineData("Polo Park", 26)]
    public async Task ReturnBadRequestForInvalidQuery(string? query, int limit)
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Search(
            new SearchLocationsRequest(query, Limit: limit),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        await context.ReadModels.DidNotReceive().SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>());
    }
}
