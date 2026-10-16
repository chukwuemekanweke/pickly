using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Nearby;

public sealed partial class When_GettingNearbyLocations_WithNearbyRequest_Should
{
    [Fact]
    public async Task ReturnNearbyLocationsWithDistances()
    {
        var context = new LocationsControllerTestContext();
        var anchor = LocationsControllerTestContext.ReadModel(type: LocationType.Address);
        var nearby = new NearbyLocationReadModel(LocationsControllerTestContext.ReadModel(), 512.4);
        context.ReadModels.GetByIdAsync(anchor.Id, Arg.Any<CancellationToken>()).Returns(anchor);
        context.ReadModels.FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>())
            .Returns([nearby]);

        var result = await context.CreateController().Nearby(
            new GetNearbyLocationsRequest(anchor.Id, 1000, 10),
            CancellationToken.None);

        var item = result.Result.ShouldBeOfType<OkObjectResult>().Value
            .ShouldBeOfType<LocationListResponse<NearbyLocationResponse>>().Items.ShouldHaveSingleItem();
        item.Id.ShouldBe(nearby.Location.Id);
        item.DistanceMeters.ShouldBe(512);
    }
}
