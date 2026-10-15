using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task RequireAuthenticationForLocationEndpoints()
    {
        var authorization = Client.DefaultRequestHeaders.Authorization;
        Client.DefaultRequestHeaders.Authorization = null;

        using var search = await Client.GetAsync($"{EndpointUrl.Locations.SearchV1}?q=polo");
        using var resolve = await Client.PostAsJsonAsync(EndpointUrl.Locations.ResolveV1, ResolveRequest("1 Test Road"));
        Client.DefaultRequestHeaders.Authorization = authorization;

        search.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        resolve.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        LocationGeocodingProvider.CallCount.ShouldBe(0);
    }
}
