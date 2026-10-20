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
            $"{EndpointUrl.Locations.SearchV1}?q={Uri.EscapeDataString($"MALL {_run.ToUpperInvariant()}")}");
        var landmarks = await landmarkResponse.Content.ReadFromJsonAsync<LocationListResponse<LocationSearchItemResponse>>();

        landmarkResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        landmarks!.Items.ShouldHaveSingleItem().LocationId.ShouldBe(_poloPark.Id);
        LocationGeocodingProvider.CallCount.ShouldBe(0);

        using var fullAddressResponse = await Client.GetAsync(
            $"{EndpointUrl.Locations.SearchV1}?q={Uri.EscapeDataString(_poloPark.Address.DisplayAddress)}");
        var fullAddressMatches = await fullAddressResponse.Content
            .ReadFromJsonAsync<LocationListResponse<LocationSearchItemResponse>>();

        fullAddressResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        fullAddressMatches!.Items.ShouldHaveSingleItem().LocationId.ShouldBe(_poloPark.Id);
        LocationGeocodingProvider.CallCount.ShouldBe(0);

        using var premiseResponse = await Client.GetAsync($"{EndpointUrl.Locations.SearchV1}?q=hidden%20{_run}");
        var premises = await premiseResponse.Content.ReadFromJsonAsync<LocationListResponse<LocationSearchItemResponse>>();

        premises!.Items.ShouldBeEmpty();
        LocationGeocodingProvider.CallCount.ShouldBe(1);
    }
}
