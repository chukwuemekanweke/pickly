using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task FindNearbyPublicLocationsWithDistancesInMetres()
    {
        using var response = await Client.GetAsync(
            $"{EndpointUrl.Locations.NearbyV1}?locationId={_poloPark.Id}&radiusMeters=1000&limit=25");
        var payload = await response.Content.ReadFromJsonAsync<LocationListResponse<NearbyLocationResponse>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        var northGate = payload.Items.Where(item => item.Id == _northGate.Id).ShouldHaveSingleItem();
        northGate.LocationType.ShouldBe(LocationType.Landmark);
        // 0.001 degrees of latitude is roughly 110.6 m near Enugu; a degree-based "distance" would be ~0.001.
        northGate.DistanceMeters.ShouldBeInRange(105, 116);
        payload.Items.ShouldNotContain(item => item.Id == _poloPark.Id);
        payload.Items.ShouldNotContain(item => item.Id == _farLandmark.Id);
        payload.Items.ShouldNotContain(item => item.Id == _privatePremise.Id);
        payload.Items.Select(item => item.DistanceMeters).ShouldBeInOrder(SortDirection.Ascending);
    }
}
