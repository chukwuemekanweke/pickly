using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Infrastructure.Locations;
using Shouldly;
using System.Text.Json;
using System.Web;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed class When_ParsingAdditionalGeocodingProviders_Should
{
    [Fact]
    public void MapGeoapifyStreetWithConfidenceAndStablePlaceIdentity()
    {
        using var document = JsonDocument.Parse(
            """
            {"features":[{"geometry":{"coordinates":[7.5,6.4]},"properties":{"place_id":"geo-1","result_type":"street","formatted":"Ogui Road, Enugu, Nigeria","street":"Ogui Road","city":"Enugu","state":"Enugu","country_code":"ng","rank":{"confidence":0.96}}}]}
            """);

        var result = GeoapifyFeatureParser.Parse(document.RootElement);
        var candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.FeatureId.ShouldBe("geo-1");
        candidate.Precision.ShouldBe(GeocodingPrecision.Street);
        candidate.Confidence.ShouldBe(GeocodingMatchConfidence.High);
        candidate.CountryCode.ShouldBe("NG");
    }

    [Fact]
    public void PreserveLocationIqUncertaintyAndOsmIdentity()
    {
        using var document = JsonDocument.Parse(
            """
            [{"place_id":"12","osm_type":"way","osm_id":"34","lat":"6.4","lon":"7.5","display_name":"Ogui Road, Enugu, Nigeria","class":"highway","type":"residential","address":{"road":"Ogui Road","city":"Enugu","state":"Enugu","country_code":"ng"}}]
            """);

        var result = LocationIqFeatureParser.Parse(document.RootElement);
        var candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.FeatureId.ShouldBe("way/34");
        candidate.Precision.ShouldBe(GeocodingPrecision.Street);
        candidate.Confidence.ShouldBe(GeocodingMatchConfidence.Unknown);
    }

    [Fact]
    public void BuildProviderSpecificCountryRestrictedRequests()
    {
        var request = new GeocodingRequest("Ogui Road", "Enugu", "Enugu", "NG", 4);
        var geoapify = HttpUtility.ParseQueryString(new Uri("https://example.test/" + GeoapifyGeocodingClient.BuildRequestUri(request, "secret")).Query);
        var locationIq = HttpUtility.ParseQueryString(new Uri("https://example.test/" + LocationIqGeocodingClient.BuildRequestUri(request, "secret")).Query);

        geoapify["filter"].ShouldBe("countrycode:ng");
        geoapify["street"].ShouldBe("Ogui Road");
        geoapify["limit"].ShouldBe("4");
        locationIq["countrycodes"].ShouldBe("ng");
        locationIq["street"].ShouldBe("Ogui Road");
        locationIq["normalizeaddress"].ShouldBe("1");
    }
}
