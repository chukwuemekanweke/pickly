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
        var request = ResolveRequest($"12 Ekpulechi Avenue {_run}");

        var first = await ResolveAsync(request);
        var second = await ResolveAsync(request);

        first.Status.ShouldBe(ResolveLocationResponse.ResolvedStatus);
        first.ResolutionSource.ShouldBe(LocationResolutionSource.Mapbox);
        first.LocationType.ShouldBe(LocationType.Address);
        second.LocationId.ShouldBe(first.LocationId);
        LocationGeocodingProvider.CallCount.ShouldBe(1);

        using var getResponse = await Client.GetAsync(EndpointUrl.Locations.ByIdV1(first.LocationId!.Value));
        var location = await getResponse.Content.ReadFromJsonAsync<LocationResponse>();
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        location!.Coordinates.ShouldBe(new LocationCoordinatesResponse(6.4512m, 7.5221m));
    }
}
