using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
[Theory]
    [InlineData("address", GeocodingPrecision.Premise)]
    [InlineData("secondary_address", GeocodingPrecision.Premise)]
    [InlineData("street", GeocodingPrecision.Street)]
    [InlineData("neighborhood", GeocodingPrecision.Area)]
    [InlineData("locality", GeocodingPrecision.Area)]
    [InlineData("place", GeocodingPrecision.Area)]
    [InlineData("region", GeocodingPrecision.Region)]
    [InlineData("country", GeocodingPrecision.Region)]
    public async Task ClassifyFeaturePrecision(string featureType, GeocodingPrecision expected)
    {
        var body = MapboxGeocodingTestContext.AddressFeatureCollection.Replace("\"feature_type\": \"address\"", $"\"feature_type\": \"{featureType}\"");
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(MapboxGeocodingTestContext.Json(body))
        };

        var result = await context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Candidates.ShouldHaveSingleItem().Precision.ShouldBe(expected);
    }
}
