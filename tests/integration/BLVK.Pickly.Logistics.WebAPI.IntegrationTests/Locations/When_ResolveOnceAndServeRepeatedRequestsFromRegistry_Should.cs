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
    public async Task ResolveOnceAndServeRepeatedRequestsFromRegistry()
    {
        LocationGeocodingProvider.Responder = (request, _) => Task.FromResult(Success(Candidate($"{_run}-addr-12", request.AddressLine)));
        var query = $"12 Ekpulechi Avenue {_run}";

        var firstSearch = await SearchAsync(query);
        var candidate = firstSearch.Items.Single(item => item.SelectionToken is not null);
        using var registration = await Client.PostAsJsonAsync(
            EndpointUrl.Locations.V1,
            new RegisterLocationRequest(candidate.SelectionToken!));
        var first = await registration.Content.ReadFromJsonAsync<LocationResponse>();
        var second = await SearchAsync(query);

        registration.StatusCode.ShouldBe(HttpStatusCode.Created);
        first!.ResolutionSource.ShouldBe(LocationResolutionSource.Mapbox);
        first.LocationType.ShouldBe(LocationType.Street);
        second.Items.ShouldContain(item => item.LocationId == first.LocationId);
        LocationGeocodingProvider.CallCount.ShouldBe(1);

        using var getResponse = await Client.GetAsync(EndpointUrl.Locations.ByIdV1(first.LocationId));
        var location = await getResponse.Content.ReadFromJsonAsync<LocationResponse>();
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        location!.Coordinates.ShouldBe(new LocationCoordinatesResponse(6.4512m, 7.5221m));
    }
}
