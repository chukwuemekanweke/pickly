using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
[Fact]
    public async Task ReturnSuccessWithoutCandidatesWhenNothingMatches()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(MapboxGeocodingTestContext.Json("""{"type":"FeatureCollection","features":[]}"""))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.Success);
        result.Candidates.ShouldBeEmpty();
    }
}
