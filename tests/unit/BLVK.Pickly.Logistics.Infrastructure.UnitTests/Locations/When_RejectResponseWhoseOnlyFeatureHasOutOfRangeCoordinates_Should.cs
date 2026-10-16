using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
[Fact]
    public async Task RejectResponseWhoseOnlyFeatureHasOutOfRangeCoordinates()
    {
        var invalid = MapboxGeocodingTestContext.AddressFeatureCollection.Replace("[7.5221, 6.4512]", "[7.5221, 96.4512]");
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(MapboxGeocodingTestContext.Json(invalid))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.InvalidResponse);
        result.Candidates.ShouldBeEmpty();
    }
}
