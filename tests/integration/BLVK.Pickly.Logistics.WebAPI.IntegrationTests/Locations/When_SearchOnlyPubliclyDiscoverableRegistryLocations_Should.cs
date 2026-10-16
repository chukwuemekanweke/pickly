using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
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
        LocationGeocodingProvider.Responder = (_, _) => Task.FromResult(Success());
        using var landmarkResponse = await Client.GetAsync(
            $"{EndpointUrl.Locations.SearchV1}?q={Uri.EscapeDataString(_poloPark.Address.DisplayAddress)}");
        var landmarks = await landmarkResponse.Content.ReadFromJsonAsync<LocationListResponse<LocationSearchItemResponse>>();
        using var premiseResponse = await Client.GetAsync($"{EndpointUrl.Locations.SearchV1}?q=hidden%20{_run}");
        var premises = await premiseResponse.Content.ReadFromJsonAsync<LocationListResponse<LocationSearchItemResponse>>();

        landmarkResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        landmarks!.Items.ShouldHaveSingleItem().LocationId.ShouldBe(_poloPark.Id);
        premises!.Items.ShouldBeEmpty();
        LocationGeocodingProvider.CallCount.ShouldBe(1);
    }
}
