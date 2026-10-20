using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
    [Fact]
    public async Task RecordRegistryHitSearchCountsAndActualTerms()
    {
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([PublicTelemetryLocation(LocationType.Street)]);

        await Handler().HandleAsync(
            new SearchLocationsQuery("17 New Haven Avenue", "NG", 10, _actor),
            CancellationToken.None);

        _telemetry.Received().AddCustomEvent(
            Observability.EventNames.Locations.RegistryHit,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Locations.RegistryOutcome] == "selectable_matches" &&
                properties[Observability.PropertyNames.Locations.RegistryResultCount] == "1" &&
                properties[Observability.PropertyNames.Locations.SelectableRegistryResultCount] == "1" &&
                properties[Observability.PropertyNames.Locations.SearchTerm] == "17 New Haven Avenue" &&
                properties[Observability.PropertyNames.Locations.NormalizedSearchTerm] == "17 new haven avenue"));
        _telemetry.Received().AddCustomEvent(
            Observability.EventNames.Locations.SearchPerformed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Locations.ResultCount] == "1" &&
                properties[Observability.PropertyNames.Locations.ResultSource] == "registry"));
    }

    [Fact]
    public async Task DistinguishRegistryMatchesThatAreNotSelectable()
    {
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([PublicTelemetryLocation(LocationType.Area)]);

        await Handler().HandleAsync(
            new SearchLocationsQuery("New Haven", "NG", 10, _actor),
            CancellationToken.None);

        _telemetry.Received().AddCustomEvent(
            Observability.EventNames.Locations.RegistryNoSelectableMatches,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Locations.RegistryOutcome] == "no_selectable_matches" &&
                properties[Observability.PropertyNames.Locations.RegistryResultCount] == "1" &&
                properties[Observability.PropertyNames.Locations.SelectableRegistryResultCount] == "0"));
    }

    private static LocationReadModel PublicTelemetryLocation(LocationType locationType) =>
        new(
            Guid.CreateVersion7(),
            "New Haven Avenue, Enugu",
            "New Haven Avenue",
            "Enugu",
            "Enugu",
            "NG",
            6.45m,
            7.52m,
            locationType,
            LocationResolutionSource.InternallyRegistered,
            LocationResolutionStatus.Resolved,
            LocationVisibility.Public);
}
