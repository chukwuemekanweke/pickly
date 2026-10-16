using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
    [Fact]
    public async Task ReturnRateLimitedWithRetryHintWithoutRetrying()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(17));
                return Task.FromResult(response);
            }
        };

        var result = await context.CreateProvider()
            .ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.RateLimited);
        result.RetryAfter.ShouldBe(TimeSpan.FromSeconds(17));
        context.Requests.Count.ShouldBe(1);
    }
}
