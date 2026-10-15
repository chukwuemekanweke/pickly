using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Fact]
    public async Task ReportTransportFailureAsUnavailable()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => throw new HttpRequestException("connection refused")
        };

        var result = await context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.ProviderUnavailable);
    }
}
