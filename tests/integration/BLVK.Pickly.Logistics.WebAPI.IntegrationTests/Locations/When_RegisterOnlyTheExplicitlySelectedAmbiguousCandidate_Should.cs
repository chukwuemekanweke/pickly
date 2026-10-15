using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task RegisterOnlyTheExplicitlySelectedAmbiguousCandidate()
    {
        LocationGeocodingProvider.Responder = (_, _) => Task.FromResult(Success(
            Candidate($"{_run}-addr-a", $"5 Ambiguous Road {_run}", GeocodingMatchConfidence.Medium),
            Candidate($"{_run}-addr-b", $"5 Ambiguous Street {_run}", GeocodingMatchConfidence.Medium, 6.4600m)));

        var ambiguous = await ResolveAsync(ResolveRequest($"5 Ambiguous {_run}"));
        using var selection = await Client.PostAsJsonAsync(
            EndpointUrl.Locations.ResolveSelectionsV1,
            new SelectLocationCandidateRequest(ambiguous.Candidates[1].SelectionToken));
        var selected = await selection.Content.ReadFromJsonAsync<ResolveLocationResponse>();

        ambiguous.Status.ShouldBe(ResolveLocationResponse.SelectionRequiredStatus);
        ambiguous.Candidates.Count.ShouldBe(2);
        selection.StatusCode.ShouldBe(HttpStatusCode.OK);
        selected!.Status.ShouldBe(ResolveLocationResponse.ResolvedStatus);
        selected.Coordinates!.Latitude.ShouldBe(6.4600m);
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.LocationProviderReferences.AnyAsync(reference => reference.FeatureId == $"{_run}-addr-a")).ShouldBeFalse();
        (await dbContext.LocationProviderReferences.AnyAsync(reference => reference.FeatureId == $"{_run}-addr-b")).ShouldBeTrue();
    }
}
