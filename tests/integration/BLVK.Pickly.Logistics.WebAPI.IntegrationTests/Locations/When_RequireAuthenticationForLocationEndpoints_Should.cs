using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
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
        using var register = await Client.PostAsJsonAsync(
            EndpointUrl.Locations.V1,
            new RegisterLocationRequest("invalid"));
        Client.DefaultRequestHeaders.Authorization = authorization;

        search.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        register.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        LocationGeocodingProvider.CallCount.ShouldBe(0);
    }
}
