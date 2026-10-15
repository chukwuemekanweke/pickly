using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
[Theory]
    [InlineData("""{"unexpected":true}""")]
    [InlineData("""not json""")]
    public async Task RejectMalformedResponse(string body)
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(MapboxGeocodingTestContext.Json(body))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.InvalidResponse);
    }
}
