using BLVK.Pickly.Logistics.Application.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.GetById;

public sealed partial class When_GettingLocation_WithLocationId_Should
{
    [Fact]
    public async Task ReturnRegisteredLocation()
    {
        var context = new LocationsControllerTestContext();
        var location = LocationsControllerTestContext.ReadModel();
        context.ReadModels.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await context.CreateController().GetById(location.Id, CancellationToken.None);

        var response = result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<LocationResponse>();
        response.LocationId.ShouldBe(location.Id);
        response.Coordinates.Latitude.ShouldBe(location.Latitude);
    }
}
