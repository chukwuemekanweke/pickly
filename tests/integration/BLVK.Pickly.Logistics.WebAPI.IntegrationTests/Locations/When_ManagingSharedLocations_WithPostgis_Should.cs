using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

[Collection(nameof(ContainersCollection))]
public sealed partial class When_ManagingSharedLocations_WithPostgis_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    // Unique per test run so registry text search, provider references and resolution keys never collide with
    // records created by other scenarios sharing the container.
    private readonly string _run = $"run{Guid.CreateVersion7():N}"[..15];
    private readonly List<Guid> _locationIds = [];
    private readonly List<SeededSender> _senders = [];
    private SeededSender _sender = default!;
    private Location _poloPark = default!;
    private Location _northGate = default!;
    private Location _farLandmark = default!;
    private Location _privatePremise = default!;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _sender = await CreateSenderAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _sender.TenantId.ToString());
        await AuthenticateAsync(_sender.Email);

        _poloPark = Landmark($"Polo Park Mall {_run}", 6.4420m, 7.5005m);
        _northGate = Landmark($"Polo Park North Gate {_run}", 6.4430m, 7.5005m);
        _farLandmark = Landmark($"Nike Lake Resort {_run}", 6.6000m, 7.6000m);
        _privatePremise = Location.RegisterProviderResolved(
            LocationAddress.Create($"7 Hidden Close {_run}", "Enugu", "Enugu", "NG"),
            GeographicCoordinates.Create(6.4421m, 7.5006m),
            LocationVisibility.Private,
            LocationResolutionSource.Mapbox,
            "mapbox",
            $"{_run}-premise",
            "address",
            RegisteredAt);
        await SaveLocationsAsync(_poloPark, _northGate, _farLandmark, _privatePremise);
    }

    public async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var runLocationIds = await dbContext.LocationProviderReferences
                .IgnoreQueryFilters()
                .Where(reference => reference.FeatureId.StartsWith(_run))
                .Select(reference => reference.LocationId)
                .ToListAsync();
            var locationIds = _locationIds.Concat(runLocationIds).Distinct().ToList();

            await dbContext.SavedPickupLocations.IgnoreQueryFilters()
                .Where(location => locationIds.Contains(location.LocationId))
                .ExecuteDeleteAsync();
            await dbContext.LocationResolutionKeys.IgnoreQueryFilters()
                .Where(key => locationIds.Contains(key.LocationId))
                .ExecuteDeleteAsync();
            await dbContext.LocationProviderReferences.IgnoreQueryFilters()
                .Where(reference => locationIds.Contains(reference.LocationId))
                .ExecuteDeleteAsync();
            await dbContext.Locations.IgnoreQueryFilters()
                .Where(location => locationIds.Contains(location.Id))
                .ExecuteDeleteAsync();
        }

        foreach (var sender in _senders)
        {
            using var scope = CreateScope();
            await IntegrationSenderSeeder.DeleteAsync(scope.ServiceProvider, sender);
        }

        await DisposeClientAsync();
    }

    [Fact]
    public async Task ProvidePostgisGeographyColumnAndSpatialIndexes()
    {
        using var scope = CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;

        var postgisVersion = await database
            .SqlQuery<string>($"""SELECT extversion AS "Value" FROM pg_extension WHERE extname = 'postgis'""")
            .SingleOrDefaultAsync();
        var columnType = await database
            .SqlQuery<string>(
                $"""
                SELECT format_type(a.atttypid, a.atttypmod) AS "Value"
                FROM pg_attribute a
                JOIN pg_class c ON c.oid = a.attrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'locations' AND c.relname = 'Locations' AND a.attname = 'Coordinates'
                """)
            .SingleAsync();
        var indexes = await database
            .SqlQuery<string>(
                $"""SELECT indexdef AS "Value" FROM pg_indexes WHERE schemaname = 'locations' AND tablename = 'Locations'""")
            .ToListAsync();

        postgisVersion.ShouldNotBeNullOrWhiteSpace();
        columnType.ShouldBe("geography(Point,4326)");
        indexes.ShouldContain(index => index.Contains("\"IX_Locations_Coordinates\"") && index.Contains("USING gist"));
        indexes.ShouldContain(index => index.Contains("\"IX_Locations_DisplayAddress_FullText\"") && index.Contains("USING gin"));
    }

    private Location Landmark(string name, decimal latitude, decimal longitude) =>
        Location.RegisterLandmark(
            LocationAddress.Create(name, "Enugu", "Enugu", "NG"),
            GeographicCoordinates.Create(latitude, longitude),
            RegisteredAt);

    private static Location ProviderLocation(string line, string featureId) =>
        Location.RegisterProviderResolved(
            LocationAddress.Create(line, "Enugu", "Enugu", "NG"),
            GeographicCoordinates.Create(6.45m, 7.52m),
            LocationVisibility.Private,
            LocationResolutionSource.Mapbox,
            "mapbox",
            featureId,
            "address",
            RegisteredAt);

    private static GeocodingResult Success(params GeocodingCandidate[] candidates) =>
        new(GeocodingStatus.Success, candidates);

    private static GeocodingCandidate Candidate(
        string featureId,
        string addressLine,
        GeocodingMatchConfidence confidence = GeocodingMatchConfidence.Exact,
        decimal latitude = 6.4512m) =>
        new(
            featureId,
            "address",
            GeocodingPrecision.Premise,
            confidence,
            $"{addressLine}, Thinkers Corner, Enugu",
            addressLine,
            "Enugu",
            "Enugu",
            "NG",
            latitude,
            7.5221m);

    private async Task<LocationListResponse<LocationSearchItemResponse>> SearchAsync(string query)
    {
        using var response = await Client.GetAsync(
            $"{EndpointUrl.Locations.SearchV1}?q={Uri.EscapeDataString(query)}&countryCode=NG");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LocationListResponse<LocationSearchItemResponse>>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private async Task SaveLocationsAsync(params Location[] locations)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Locations.AddRange(locations);
        await dbContext.SaveChangesAsync();
        _locationIds.AddRange(locations.Select(location => location.Id));
    }

    private async Task<SeededSender> CreateSenderAsync()
    {
        using var scope = CreateScope();
        var sender = await IntegrationSenderSeeder.CreateVerifiedSenderAsync(scope.ServiceProvider, Guid.CreateVersion7());
        _senders.Add(sender);
        return sender;
    }

    private async Task<CreatePickupLocationResult> CreatePickupThroughHandlerAsync(
        SeededSender sender,
        Guid locationId,
        string label,
        string? directions)
    {
        using var scope = CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<CreatePickupLocationHandler>().HandleAsync(
            new CreatePickupLocationCommand(locationId, label, directions, false, Actor(sender)),
            CancellationToken.None);
        result.Status.ShouldBe(CreatePickupLocationStatus.Success);
        return result;
    }

    private static ActorContext Actor(SeededSender sender) =>
        new(sender.StakeholderId, sender.TenantId, "correlation", "flow");

    private async Task AuthenticateAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(email, IntegrationSenderSeeder.Password));
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }
}
