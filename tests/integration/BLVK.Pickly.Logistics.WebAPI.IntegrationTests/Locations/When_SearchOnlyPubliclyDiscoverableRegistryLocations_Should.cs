using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task SearchOnlyPubliclyDiscoverableRegistryLocations()
    {
        using var landmarkResponse = await Client.GetAsync($"{EndpointUrl.Locations.SearchV1}?q={_run}%20polo");
        var landmarks = await landmarkResponse.Content.ReadFromJsonAsync<LocationListResponse<LocationSummaryResponse>>();
        using var premiseResponse = await Client.GetAsync($"{EndpointUrl.Locations.SearchV1}?q=hidden%20{_run}");
        var premises = await premiseResponse.Content.ReadFromJsonAsync<LocationListResponse<LocationSummaryResponse>>();

        landmarkResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        landmarks!.Items.Select(item => item.Id).ShouldBe([_poloPark.Id, _northGate.Id], ignoreOrder: true);
        premises!.Items.ShouldBeEmpty();
        LocationGeocodingProvider.CallCount.ShouldBe(0);
    }
}
