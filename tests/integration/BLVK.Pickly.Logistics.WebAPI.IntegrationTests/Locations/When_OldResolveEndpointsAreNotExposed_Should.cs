using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
    [Fact]
    public async Task OldResolveEndpointsAreNotExposed()
    {
        using var resolve = await Client.PostAsJsonAsync($"{EndpointUrl.Locations.V1}/resolve", new { });
        using var selection = await Client.PostAsJsonAsync($"{EndpointUrl.Locations.V1}/resolve/selections", new { });

        resolve.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        selection.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
