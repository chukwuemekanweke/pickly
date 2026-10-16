using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithoutPreciseProviderMatch_Should
{
[Fact]
    public async Task SuggestKnownLandmarksAroundCoarseProviderAnchorWithoutPersistingIt()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate(
            featureId: "locality.thinkers-corner",
            addressLine: "Thinkers Corner",
            precision: GeocodingPrecision.Area,
            latitude: 6.4601m,
            longitude: 7.5302m));
        var landmark = new NearbyLocationReadModel(
            new LocationReadModel(
                Guid.CreateVersion7(),
                "Thinkers Corner Roundabout, Enugu",
                "Thinkers Corner Roundabout",
                "Enugu",
                "Enugu",
                "NG",
                6.4610m,
                7.5310m,
                LocationType.Landmark,
                LocationResolutionSource.InternallyRegistered,
                LocationResolutionStatus.Resolved,
                LocationVisibility.Public),
            132.4);
        NearbyLocationsRequest? captured = null;
        context.ReadModels.FindNearbyAsync(Arg.Do<NearbyLocationsRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns([landmark]);

        var result = await context.ResolveHandler().HandleAsync(context.Command("Unnamed close"), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Unresolved);
        result.NearbyLandmarks!.ShouldHaveSingleItem().Id.ShouldBe(landmark.Location.Id);
        result.NearbyLandmarks![0].DistanceMeters.ShouldBe(132);
        captured.ShouldNotBeNull();
        captured.LocationType.ShouldBe(LocationType.Landmark);
        captured.RadiusMeters.ShouldBe(context.Settings.LandmarkFallbackRadiusMeters);
        captured.Anchor.Latitude.ShouldBe(6.4601m);
        captured.Anchor.Longitude.ShouldBe(7.5302m);
        context.LocationItems.ShouldBeEmpty();
    }
}
