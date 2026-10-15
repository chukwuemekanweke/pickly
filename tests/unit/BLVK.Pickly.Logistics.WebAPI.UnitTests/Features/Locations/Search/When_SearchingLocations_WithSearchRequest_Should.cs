using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Search;

public sealed partial class When_SearchingLocations_WithSearchRequest_Should
{
    [Fact]
    public async Task ReturnRegisteredMatchesWithoutGeocoding()
    {
        var context = new LocationsControllerTestContext();
        var location = LocationsControllerTestContext.ReadModel();
        context.ReadModels.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([location]);

        var result = await context.CreateController().Search(
            new SearchLocationsRequest("Polo Park"),
            CancellationToken.None);

        var response = result.Result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBeOfType<LocationListResponse<LocationSummaryResponse>>();
        response.Items.ShouldHaveSingleItem().Id.ShouldBe(location.Id);
        await context.GeocodingProvider.DidNotReceive()
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }
}
