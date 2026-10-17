using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

public sealed class When_BuildingMapboxRouteRequest_Should
{
    [Fact]
    public void PutLongitudeBeforeLatitude_AndSuppressRouteGeometry()
    {
        var request = new DeliveryRouteRequest(Guid.CreateVersion7(), GeographicCoordinates.FromDegrees(6.4483, 7.5139), Guid.CreateVersion7(), GeographicCoordinates.FromDegrees(6.4550, 7.5200), "driving", DateTimeOffset.UtcNow);

        var uri = MapboxDeliveryRouteProvider.BuildUri(request, "driving", "secret token");

        uri.ShouldStartWith("directions/v5/mapbox/driving/7.5139,6.4483;7.52,6.455?");
        uri.ShouldContain("overview=false");
        uri.ShouldContain("access_token=secret%20token");
    }
}
