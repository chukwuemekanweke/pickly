using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;
using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
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
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations;

internal sealed class LocationsControllerTestContext
{
    public LocationsControllerTestContext()
    {
        GeocodingProvider = Substitute.For<ILocationGeocodingProvider>();
        GeocodingProvider.ProviderName.Returns("mapbox");
        GeocodingProvider.ResolutionSource.Returns(LocationResolutionSource.Mapbox);
        GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success, []));
        Lease = Substitute.For<ILocationResolutionLease>();
        Lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Guid.CreateVersion7());
        RegistrationStore = Substitute.For<ILocationRegistrationStore>();
        RegistrationStore.RegisterAsync(
                Arg.Any<Location>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => new LocationRegistration(callInfo.ArgAt<Location>(0), true));
        ReadModels = Substitute.For<ILocationReadModelRepository>();
        ReadModels.FindNearbyAsync(Arg.Any<NearbyLocationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NearbyLocationReadModel>());
        ReadModels.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LocationReadModel>());
        Cache = Substitute.For<IJsonCache>();
        Telemetry = Substitute.For<ICustomTelemetryContext>();
        CurrentActor = Substitute.For<ICurrentActor>();
        CurrentActor.ActorId.Returns(StakeholderId.ToString());
        CurrentActor.TenantId.Returns(Guid.CreateVersion7());
        CurrentActor.CorrelationId.Returns("correlation");
        CurrentActor.FlowId.Returns("flow");
    }

    public Guid StakeholderId { get; } = Guid.CreateVersion7();
    public ILocationGeocodingProvider GeocodingProvider { get; }
    public ILocationResolutionLease Lease { get; }
    public ILocationRegistrationStore RegistrationStore { get; }
    public ILocationReadModelRepository ReadModels { get; }
    public IJsonCache Cache { get; }
    public ICustomTelemetryContext Telemetry { get; }
    public ICurrentActor CurrentActor { get; }
    public LocationResolutionOptions Settings { get; } = new()
    {
        ResolutionLeaseWaitTimeout = TimeSpan.Zero,
        ResolutionLeasePollInterval = TimeSpan.FromMilliseconds(50)
    };

    public LocationsController CreateController()
    {
        var options = Options.Create(Settings);
        var registrar = new LocationCandidateRegistrar(RegistrationStore, Telemetry, TimeProvider.System);
        var resolveTelemetry = new ResolveLocationTelemetry(Telemetry);
        return new LocationsController(
            new SearchLocationsHandler(
                ReadModels,
                GeocodingProvider,
                Lease,
                Cache,
                resolveTelemetry,
                options,
                TimeProvider.System),
            new SelectLocationCandidateHandler(Cache, registrar, Telemetry),
            new GetLocationHandler(ReadModels),
            new GetNearbyLocationsHandler(ReadModels, options),
            new SearchLocationsRequestValidator(options),
            new RegisterLocationRequestValidator(),
            new GetNearbyLocationsRequestValidator(options),
            CurrentActor)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    public void ProviderReturns(params GeocodingCandidate[] candidates) =>
        GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success, candidates));

    public void ProviderFails(GeocodingStatus status, TimeSpan? retryAfter = null) =>
        GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingResult.Failed(status, retryAfter));

    public static GeocodingCandidate Candidate(
        string featureId = "address.12",
        GeocodingPrecision precision = GeocodingPrecision.Premise,
        GeocodingMatchConfidence confidence = GeocodingMatchConfidence.Exact) =>
        new(
            featureId,
            "address",
            precision,
            confidence,
            "12 Ekpulechi Avenue, Thinkers Corner, Enugu",
            "12 Ekpulechi Avenue",
            "Enugu",
            "Enugu",
            "NG",
            6.4512m,
            7.5221m);

    public static LocationReadModel ReadModel(Guid? id = null, LocationType type = LocationType.Landmark) =>
        new(
            id ?? Guid.CreateVersion7(),
            "Polo Park Mall, Enugu",
            "Polo Park Mall",
            "Enugu",
            "Enugu",
            "NG",
            6.4420m,
            7.5005m,
            type,
            LocationResolutionSource.InternallyRegistered,
            LocationResolutionStatus.Resolved,
            LocationVisibility.Public);
}
