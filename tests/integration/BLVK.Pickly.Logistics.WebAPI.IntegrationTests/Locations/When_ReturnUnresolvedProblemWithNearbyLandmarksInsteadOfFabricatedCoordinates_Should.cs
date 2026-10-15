using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task ReturnUnresolvedProblemWithNearbyLandmarksInsteadOfFabricatedCoordinates()
    {
        LocationGeocodingProvider.Responder = (_, _) => Task.FromResult(Success(new GeocodingCandidate(
            $"{_run}-locality",
            "locality",
            GeocodingPrecision.Area,
            GeocodingMatchConfidence.High,
            "Independence Layout, Enugu",
            "Independence Layout",
            "Enugu",
            "Enugu",
            "NG",
            6.4425m,
            7.5008m)));

        using var response = await Client.PostAsJsonAsync(
            EndpointUrl.Locations.ResolveV1,
            ResolveRequest($"Behind the mechanic {_run}"));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        problem.GetProperty("code").GetString().ShouldBe(LocationErrorCodes.Unresolved);
        var landmarkIds = problem.GetProperty("nearbyLandmarks").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();
        landmarkIds.ShouldContain(_poloPark.Id);
        landmarkIds.ShouldContain(_northGate.Id);
        landmarkIds.ShouldNotContain(_privatePremise.Id);
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.LocationProviderReferences.AnyAsync(reference => reference.FeatureId == $"{_run}-locality")).ShouldBeFalse();
    }
}
