using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Fact]
    public async Task RejectUnauthorizedTokenAsUnavailable()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.ProviderUnavailable);
        context.Requests.Count.ShouldBe(1);
    }
}
