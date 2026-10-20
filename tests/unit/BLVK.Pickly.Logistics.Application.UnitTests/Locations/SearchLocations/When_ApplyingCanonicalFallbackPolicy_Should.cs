using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
    [Fact]
    public async Task ReturnAreaWithoutRegistrationToken()
    {
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success,
            [
                LocationResolutionTestContext.Candidate(
                    featureId: "locality.ogui",
                    addressLine: "Ogui",
                    precision: GeocodingPrecision.Area)
            ]));

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("19 Unknown Street, Ogui, Enugu", "NG", 10, _actor),
            CancellationToken.None);

        var area = result.Items.ShouldHaveSingleItem();
        area.IsSelectable.ShouldBeFalse();
        area.LocationId.ShouldBeNull();
        area.SelectionToken.ShouldBeNull();
        area.Precision.ShouldBe(GeocodingPrecision.Area);
    }

    [Fact]
    public async Task ReturnEmptyWhenProviderFindsNothingUseful()
    {
        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("Unknown", "NG", 10, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(SearchLocationsStatus.Success);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnExistingCanonicalStreetAfterBuildingSearch()
    {
        var candidate = LocationResolutionTestContext.Candidate(featureId: "street.ekpulechi");
        var existing = PublicLocation(candidate.DisplayAddress);
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GeocodingResult(GeocodingStatus.Success, [candidate]));
        _repository.FindByProviderReferenceAsync("mapbox", candidate.FeatureId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("41 Ekpulechi Avenue", "NG", 10, _actor),
            CancellationToken.None);

        var item = result.Items.ShouldHaveSingleItem();
        item.LocationId.ShouldBe(existing.Id);
        item.SelectionToken.ShouldBeNull();
        item.IsSelectable.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GeocodingStatus.RateLimited, SearchLocationsStatus.ProviderRateLimited)]
    [InlineData(GeocodingStatus.InvalidResponse, SearchLocationsStatus.InvalidProviderResponse)]
    public async Task PreserveProviderFailures(GeocodingStatus providerStatus, SearchLocationsStatus expected)
    {
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingResult.Failed(providerStatus));

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("Unknown", "NG", 10, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(expected);
    }

    [Theory]
    [InlineData(GeocodingStatus.Timeout)]
    [InlineData(GeocodingStatus.ProviderUnavailable)]
    public async Task ReturnEmptySuccessWhenProviderIsUnavailable(GeocodingStatus providerStatus)
    {
        _provider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingResult.Failed(providerStatus));

        var result = await Handler().HandleAsync(
            new SearchLocationsQuery("Unknown", "NG", 10, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(SearchLocationsStatus.Success);
        result.Items.ShouldBeEmpty();
    }
}
