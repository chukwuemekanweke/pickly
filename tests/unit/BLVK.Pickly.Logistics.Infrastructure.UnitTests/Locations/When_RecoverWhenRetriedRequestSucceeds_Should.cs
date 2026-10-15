using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Fact]
    public async Task RecoverWhenRetriedRequestSucceeds()
    {
        var attempts = 0;
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(++attempts == 1
                ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                : MapboxGeocodingTestContext.Json(MapboxGeocodingTestContext.AddressFeatureCollection))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.Success);
        context.Requests.Count.ShouldBe(2);
    }
}
