using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Theory]
    [InlineData(GeocodingStatus.Timeout, StatusCodes.Status503ServiceUnavailable, LocationErrorCodes.GeocodingUnavailable)]
    [InlineData(GeocodingStatus.ProviderUnavailable, StatusCodes.Status503ServiceUnavailable, LocationErrorCodes.GeocodingUnavailable)]
    [InlineData(GeocodingStatus.InvalidResponse, StatusCodes.Status502BadGateway, LocationErrorCodes.InvalidProviderResponse)]
    public async Task MapProviderFailureToProblem(GeocodingStatus providerStatus, int expectedStatusCode, string expectedCode)
    {
        var context = new LocationsControllerTestContext();
        context.ProviderFails(providerStatus);

        var result = await context.CreateController().Resolve(
            LocationsControllerTestContext.ResolveRequest(),
            CancellationToken.None);

        AssertProblem(result, expectedStatusCode, expectedCode);
    }
}
