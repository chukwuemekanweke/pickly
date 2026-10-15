using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Fact]
    public async Task ReturnUnprocessableForUnsupportedRegionWithoutCallingProvider()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Resolve(
            LocationsControllerTestContext.ResolveRequest("GH"),
            CancellationToken.None);

        AssertProblem(result, StatusCodes.Status422UnprocessableEntity, LocationErrorCodes.UnsupportedRegion);
        await context.GeocodingProvider.DidNotReceive()
            .ForwardGeocodePermanentAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }
}
