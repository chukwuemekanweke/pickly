using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
    [Fact]
    public async Task AvoidProviderForPartialRegistryMatch()
    {
        var location = PublicLocation("New Haven Market, Enugu");
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>()).Returns([location]);

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("new HAVEN", "NG", 10, _actor),
            CancellationToken.None);

        result.Items.ShouldHaveSingleItem().Source.ShouldBe(LocationSearchSource.Registry);
        await _provider.DidNotReceive()
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AvoidProviderForSingleSelectableRegistryMatch()
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

        result.Items.ShouldHaveSingleItem().Source.ShouldBe(LocationSearchSource.Registry);
        await _provider.DidNotReceive()
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AvoidProviderForMultipleSelectableRegistryMatchesEvenWhenUnavailable()
    {
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([
                PublicLocation("New Haven Avenue, Enugu"),
                PublicLocation("New Haven Market, Enugu", LocationType.Landmark)
            ]);
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingResult.Failed(GeocodingStatus.ProviderUnavailable));

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("New Haven", "NG", 10, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(SearchLocationsStatus.Success);
        result.Items.Count.ShouldBe(2);
        await _provider.DidNotReceive()
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallBackToProviderWhenRegistryContainsOnlyArea()
    {
        _repository.SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([PublicLocation("New Haven, Enugu", LocationType.Area)]);
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(
                GeocodingStatus.Success,
                [LocationResolutionTestContext.Candidate(addressLine: "New Haven Avenue")]));

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("New Haven", "NG", 10, _actor),
            CancellationToken.None);

        result.Items.Any(item => item.LocationType == LocationType.Area && !item.IsSelectable).ShouldBeTrue();
        result.Items.Any(item => item.SelectionToken is not null && item.IsSelectable).ShouldBeTrue();
        await _provider.Received(1)
            .ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>());
    }

    private static LocationReadModel PublicLocation(
        string displayAddress,
        LocationType locationType = LocationType.Street) =>
        new(
            Guid.CreateVersion7(),
            displayAddress,
            "Ekpulechi Avenue",
            "Enugu",
            "Enugu",
            "NG",
            6.45m,
            7.52m,
            locationType,
            LocationResolutionSource.Mapbox,
            LocationResolutionStatus.Resolved,
            LocationVisibility.Public);
}
