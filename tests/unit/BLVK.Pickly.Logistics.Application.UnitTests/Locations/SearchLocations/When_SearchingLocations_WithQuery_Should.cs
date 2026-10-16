using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
    private readonly ILocationReadModelRepository _repository = Substitute.For<ILocationReadModelRepository>();
    private readonly ILocationGeocodingProvider _provider = Substitute.For<ILocationGeocodingProvider>();
    private readonly ILocationResolutionLease _lease = Substitute.For<ILocationResolutionLease>();
    private readonly IJsonCache _cache = Substitute.For<IJsonCache>();
    private readonly ICustomTelemetryContext _telemetry = Substitute.For<ICustomTelemetryContext>();
    private readonly ActorContext _actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");

    public When_SearchingLocations_WithQuery_Should()
    {
        _provider.ProviderName.Returns("mapbox");
        _provider.ResolutionSource.Returns(LocationResolutionSource.Mapbox);
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success, []));
        _lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Guid.CreateVersion7());
    }

    [Fact]
    public async Task SearchRegistryWithSanitizedTermsAndReturnSummaries()
    {
        LocationSearchRequest? captured = null;
        var location = new LocationReadModel(
            Guid.CreateVersion7(),
            "Ekpulechi Avenue, Thinkers Corner, Enugu",
            "Ekpulechi Avenue",
            "Enugu",
            "Enugu",
            "NG",
            6.45m,
            7.52m,
            LocationType.Address,
            LocationResolutionSource.Mapbox,
            LocationResolutionStatus.Resolved,
            LocationVisibility.Public);
        _repository.SearchAsync(Arg.Do<LocationSearchRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns([location]);

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("Ekpulechi  Avenue:* | ' OR 1=1", "ng", 10, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(SearchLocationsStatus.Success);
        result.Items.ShouldHaveSingleItem().LocationId.ShouldBe(location.Id);
        captured.ShouldNotBeNull();
        captured.Terms.ShouldBe(["ekpulechi", "avenue", "or", "1"]);
        captured.CountryCode.ShouldBe("NG");
    }

    private SearchLocationsHandler Handler() => new(
        _repository,
        _provider,
        _lease,
        _cache,
        new ResolveLocationTelemetry(_telemetry),
        Options.Create(new LocationResolutionOptions()),
        TimeProvider.System);
}
