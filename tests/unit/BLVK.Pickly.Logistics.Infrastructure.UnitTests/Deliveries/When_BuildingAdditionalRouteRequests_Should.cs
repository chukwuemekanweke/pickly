using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using Shouldly;
using System.Web;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

public sealed class When_BuildingAdditionalRouteRequests_Should
{
    [Fact]
    public void UseProviderSpecificCoordinateOrderAndProfiles()
    {
        var request = MapboxDeliveryRoutingTestContext.Request();
        var geoapifyUri = new Uri("https://example.test/" + GeoapifyDeliveryRouteProvider.BuildUri(request, "cycling", "secret"));
        var locationIqUri = new Uri("https://example.test/" + LocationIqDeliveryRouteProvider.BuildUri(request, "driving", "secret"));
        var geoapifyQuery = HttpUtility.ParseQueryString(geoapifyUri.Query);

        geoapifyQuery["waypoints"].ShouldBe("6.45,7.51|6.46,7.52");
        geoapifyQuery["mode"].ShouldBe("bicycle");
        locationIqUri.AbsolutePath.ShouldBe("/directions/driving/7.51,6.45;7.52,6.46");
        HttpUtility.ParseQueryString(locationIqUri.Query)["overview"].ShouldBe("false");
    }
}
