using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Fact]
    public async Task RetryServerErrorsWithinBoundAndThenReportUnavailable()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.ProviderUnavailable);
        context.Requests.Count.ShouldBe(3);
    }
}
