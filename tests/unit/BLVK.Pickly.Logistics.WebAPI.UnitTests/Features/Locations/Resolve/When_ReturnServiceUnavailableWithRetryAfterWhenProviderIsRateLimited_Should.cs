using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Fact]
    public async Task ReturnServiceUnavailableWithRetryAfterWhenProviderIsRateLimited()
    {
        var context = new LocationsControllerTestContext();
        context.ProviderFails(GeocodingStatus.RateLimited, TimeSpan.FromSeconds(30));
        var controller = context.CreateController();

        var result = await controller.Resolve(LocationsControllerTestContext.ResolveRequest(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status503ServiceUnavailable, LocationErrorCodes.GeocodingRateLimited);
        controller.Response.Headers.RetryAfter.ToString().ShouldBe("30");
    }
}
