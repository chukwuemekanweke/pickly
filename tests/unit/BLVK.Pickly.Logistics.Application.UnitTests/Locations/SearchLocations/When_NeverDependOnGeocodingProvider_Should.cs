using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
    [Fact]
    public async Task AvoidProviderForReliableRegistryMatch()
    {
        var location = PublicLocation("12 Ekpulechi Avenue, Enugu");
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>()).Returns([location]);

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery(location.DisplayAddress, "NG", 10, _actor),
            CancellationToken.None);

        result.Items.ShouldHaveSingleItem().Source.ShouldBe(LocationSearchSource.Registry);
        await _provider.DidNotReceive()
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallBackToProviderForWeakRegistryMatchAndIssueSelectionToken()
    {
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([PublicLocation("Ekpulechi Avenue, Enugu")]);
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(
                GeocodingStatus.Success,
                [LocationResolutionTestContext.Candidate()]));

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("12 Ekpulechi Avenue", "NG", 10, _actor),
            CancellationToken.None);

        result.Items.Any(item => item.Source == LocationSearchSource.Registry).ShouldBeTrue();
        result.Items.Any(item =>
            item.Source == LocationSearchSource.Mapbox &&
            item.LocationId == null &&
            item.SelectionToken != null).ShouldBeTrue();
        await _provider.Received(1)
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }

    private static LocationReadModel PublicLocation(string displayAddress) =>
        new(
            Guid.CreateVersion7(),
            displayAddress,
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
}
