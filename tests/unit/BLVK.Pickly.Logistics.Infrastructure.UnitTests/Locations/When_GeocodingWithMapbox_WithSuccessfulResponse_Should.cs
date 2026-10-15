using Shouldly;
using System.Web;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithSuccessfulResponse_Should
{
    [Fact]
    public async Task RequestStructuredPermanentGeocodingRestrictedToCountry()
    {
        var context = new MapboxGeocodingTestContext();

        await context.CreateProvider().ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

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
}
