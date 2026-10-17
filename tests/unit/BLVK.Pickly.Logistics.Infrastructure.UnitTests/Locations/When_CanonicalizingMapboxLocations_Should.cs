using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed class When_CanonicalizingMapboxLocations_Should
{
    [Fact]
    public async Task GiveDifferentBuildingNumbersTheSameStreetIdentity()
    {
        var first = await Resolve(MapboxGeocodingTestContext.AddressFeatureCollection);
        var second = await Resolve(MapboxGeocodingTestContext.AddressFeatureCollection
            .Replace("12 Ekpulechi Avenue", "41 Ekpulechi Avenue")
            .Replace("\"address_number\": \"12\"", "\"address_number\": \"41\""));

        first.FeatureId.ShouldBe("s");
        second.FeatureId.ShouldBe(first.FeatureId);
        second.AddressLine.ShouldBe("Ekpulechi Avenue");
        second.Precision.ShouldBe(GeocodingPrecision.Street);
    }

    [Fact]
    public async Task KeepNamedLandmarkDistinctFromItsParentStreet()
    {
        var json = MapboxGeocodingTestContext.AddressFeatureCollection
            .Replace("\"feature_type\": \"address\"", "\"feature_type\": \"poi\"")
            .Replace("\"mapbox_id\": \"dXJuOm1ieGFkcjoxMg\"", "\"mapbox_id\": \"poi.bethel\"")
            .Replace("12 Ekpulechi Avenue", "Bethel Plaza");
        var landmark = await Resolve(json);

        landmark.FeatureId.ShouldBe("poi.bethel");
        landmark.Precision.ShouldBe(GeocodingPrecision.Landmark);
        landmark.FeatureId.ShouldNotBe("s");
    }

    private static async Task<GeocodingCandidate> Resolve(string response)
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = (_, _) => Task.FromResult(MapboxGeocodingTestContext.Json(response))
        };
        var result = await context.CreateProvider().ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);
        return result.Candidates.ShouldHaveSingleItem();
    }
}
