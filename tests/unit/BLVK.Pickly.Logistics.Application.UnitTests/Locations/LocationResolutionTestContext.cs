using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations;

internal sealed class LocationResolutionTestContext
{
    public const string AddressLine = "12 Ekpulechi Avenue";

    public LocationResolutionTestContext()
    {
        GeocodingProvider = Substitute.For<ILocationGeocodingProvider>();
        GeocodingProvider.ProviderName.Returns("mapbox");
        GeocodingProvider.ResolutionSource.Returns(LocationResolutionSource.Mapbox);
        GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success, []));
        Lease = Substitute.For<ILocationResolutionLease>();
        Lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Guid.CreateVersion7());
        ReadModels = Substitute.For<ILocationReadModelRepository>();
        ReadModels.FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NearbyLocationReadModel>());
        Telemetry = Substitute.For<ICustomTelemetryContext>();
        Locations = Substitute.For<IRepository<Location>>();
        Locations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => LocationItems.FirstOrDefault(location =>
                location.Id == call.ArgAt<Guid>(0) && !location.IsDeleted));
        Locations.FirstOrDefaultAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>())
            .Returns(call => Apply(call.ArgAt<ISpecification<Location>>(0)).FirstOrDefault());
        Locations.ListAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>())
            .Returns(call => Apply(call.ArgAt<ISpecification<Location>>(0)).ToArray());
        Locations.AddAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                LocationItems.Add(call.ArgAt<Location>(0));
                return Task.CompletedTask;
            });

        RegistrationStore = Substitute.For<ILocationRegistrationStore>();
        RegistrationStore.RegisterAsync(
                Arg.Any<Location>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Register(
                call.ArgAt<Location>(0),
                call.ArgAt<string>(1),
                call.ArgAt<string>(2),
                call.ArgAt<string?>(3)));

        Cache = Substitute.For<IJsonCache>();
        ConfigureCache<CachedGeocodingOutcome>();
        ConfigureCache<PendingLocationSelection>();
    }

    public IRepository<Location> Locations { get; }
    public List<Location> LocationItems { get; } = [];
    public ILocationRegistrationStore RegistrationStore { get; }
    public IJsonCache Cache { get; }
    public IReadOnlyCollection<string> CacheKeys => _cacheEntries.Keys;
    public ILocationGeocodingProvider GeocodingProvider { get; }
    public ILocationResolutionLease Lease { get; }
    public ILocationReadModelRepository ReadModels { get; }
    public ICustomTelemetryContext Telemetry { get; }
    public LocationResolutionOptions Settings { get; set; } = new()
    {
        ResolutionLeaseWaitTimeout = TimeSpan.FromMilliseconds(400),
        ResolutionLeasePollInterval = TimeSpan.FromMilliseconds(50)
    };

    public Guid StakeholderId { get; } = Guid.CreateVersion7();
    public ActorContext Actor => new(StakeholderId, Guid.CreateVersion7(), "correlation", "flow");

    private readonly Dictionary<string, string> _cacheEntries = [];

    public LocationCandidateRegistrar Registrar() =>
        new(RegistrationStore, Telemetry, TimeProvider.System);

    public ResolveLocationHandler ResolveHandler()
    {
        var resolveTelemetry = new ResolveLocationTelemetry(Telemetry);
        return new ResolveLocationHandler(
            new LocationRegistryMatcher(Locations, resolveTelemetry),
            Lease,
            new ProviderLocationResolver(
                GeocodingProvider,
                Registrar(),
                ReadModels,
                Cache,
                resolveTelemetry,
                TimeProvider.System),
            resolveTelemetry,
            Options.Create(Settings),
            TimeProvider.System);
    }

    public SelectLocationCandidateHandler SelectHandler() => new(Cache, Registrar(), Telemetry);

    public ResolveLocationCommand Command(string addressLine = AddressLine, string countryCode = "NG") =>
        new(addressLine, "Enugu", "Enugu", countryCode, Actor);

    public void ProviderReturns(params GeocodingCandidate[] candidates) =>
        GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success, candidates));

    public void ProviderFails(GeocodingStatus status, TimeSpan? retryAfter = null) =>
        GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingResult.Failed(status, retryAfter));

    public Task<GeocodingResult> ProviderCalls(int expected) =>
        GeocodingProvider.Received(expected).ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());

    public static GeocodingCandidate Candidate(
        string featureId = "address.12",
        string addressLine = AddressLine,
        GeocodingPrecision precision = GeocodingPrecision.Street,
        GeocodingMatchConfidence confidence = GeocodingMatchConfidence.Exact,
        decimal latitude = 6.4512m,
        decimal longitude = 7.5221m,
        string countryCode = "NG") =>
        new(
            featureId,
            precision switch
            {
                GeocodingPrecision.Premise => "address",
                GeocodingPrecision.Street => "street",
                GeocodingPrecision.Area => "locality",
                _ => "region"
            },
            precision,
            confidence,
            $"{addressLine}, Thinkers Corner, Enugu",
            addressLine,
            "Enugu",
            "Enugu",
            countryCode,
            latitude,
            longitude);

    public Location SeedProviderLocation(string addressLine = AddressLine, string featureId = "address.12", string? resolutionKey = null)
    {
        var location = Location.RegisterProviderResolved(
            LocationAddress.Create(addressLine, "Enugu", "Enugu", "NG"),
            GeographicCoordinates.Create(6.4512m, 7.5221m),
            LocationVisibility.Private,
            LocationResolutionSource.Mapbox,
            "mapbox",
            featureId,
            "address",
            System.TimeProvider.System.GetUtcNow());
        if (resolutionKey is not null)
        {
            location.AddResolutionKey(resolutionKey);
        }

        LocationItems.Add(location);
        return location;
    }

    public static string ResolutionKey(string addressLine = AddressLine) =>
        LocationAddress.ComputeResolutionKey(addressLine, "Enugu", "Enugu", "NG");

    private IEnumerable<Location> Apply(ISpecification<Location> specification)
    {
        IEnumerable<Location> query = LocationItems.Where(location => !location.IsDeleted);
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria.Compile());
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query.ToArray();
    }

    private LocationRegistration Register(
        Location candidate,
        string provider,
        string featureId,
        string? resolutionKey)
    {
        var existing = LocationItems.FirstOrDefault(location => location.HasProviderReference(provider, featureId));
        var target = existing ?? candidate;
        if (resolutionKey is not null &&
            target.IsUsable &&
            !LocationItems.Any(location => location.ResolutionKeys.Any(key => key.Key == resolutionKey)))
        {
            target.AddResolutionKey(resolutionKey);
        }

        if (existing is null)
        {
            LocationItems.Add(candidate);
        }

        return new LocationRegistration(target, existing is null);
    }

    private void ConfigureCache<T>()
    {
        Cache.GetAsync<T>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _cacheEntries.TryGetValue(call.ArgAt<string>(0), out var json)
                ? JsonSerializer.Deserialize<T>(json)
                : default);
        Cache.SetAsync(Arg.Any<string>(), Arg.Any<T>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _cacheEntries[call.ArgAt<string>(0)] = JsonSerializer.Serialize(call.ArgAt<T>(1));
                return Task.CompletedTask;
            });
    }
}
