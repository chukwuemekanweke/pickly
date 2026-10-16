using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;
using System.Web;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
    [Fact]
    public async Task RequestStructuredPermanentGeocodingRestrictedToCountry()
    {
        var context = new MapboxGeocodingTestContext();

        await context.CreateProvider().ForwardGeocodeAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        var request = context.Requests.ShouldHaveSingleItem();
        request.RequestUri!.AbsolutePath.ShouldBe("/search/geocode/v6/forward");
        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
        query["permanent"].ShouldBe("true");
        query["autocomplete"].ShouldBe("false");
        query["country"].ShouldBe("ng");
        query["address_line1"].ShouldBe("12 Ekpulechi Avenue");
        query["place"].ShouldBe("Enugu");
        query["region"].ShouldBe("Enugu");
        query["limit"].ShouldBe("5");
        query["access_token"].ShouldBe(MapboxGeocodingTestContext.AccessToken);
    }

    [Fact]
    public async Task RequestFreeTextPermanentGeocodingForUnifiedSearch()
    {
        var context = new MapboxGeocodingTestContext();

        await context.CreateProvider().ForwardGeocodeAsync(
            new GeocodingRequest("12 Ekpulechi Avenue Enugu", string.Empty, string.Empty, "NG", 5),
            CancellationToken.None);

        var query = HttpUtility.ParseQueryString(context.Requests.ShouldHaveSingleItem().RequestUri!.Query);
        query["q"].ShouldBe("12 Ekpulechi Avenue Enugu");
        query["address_line1"].ShouldBeNull();
        query["permanent"].ShouldBe("true");
        query["autocomplete"].ShouldBe("false");
        query["country"].ShouldBe("ng");
    }
}
