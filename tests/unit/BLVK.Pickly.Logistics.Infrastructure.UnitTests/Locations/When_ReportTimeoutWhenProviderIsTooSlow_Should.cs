using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Fact]
    public async Task ReportTimeoutWhenProviderIsTooSlow()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                return MapboxGeocodingTestContext.Json(MapboxGeocodingTestContext.AddressFeatureCollection);
            }
        }.Configure("Timeout", "00:00:00.200");

        var result = await context.CreateProvider()
            .ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.Timeout);
    }
}
