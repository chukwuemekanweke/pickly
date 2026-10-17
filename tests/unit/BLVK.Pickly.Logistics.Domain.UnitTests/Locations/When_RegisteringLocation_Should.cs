using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
    [Fact]
    public void RecordProviderResolvedAddressWithPermanentProvenance()
    {
        var location = LocationTestFactory.ProviderResolved();

        location.LocationType.ShouldBe(LocationType.Street);
        location.ResolutionSource.ShouldBe(LocationResolutionSource.Mapbox);
        location.ResolutionStatus.ShouldBe(LocationResolutionStatus.Resolved);
        location.Visibility.ShouldBe(LocationVisibility.Private);
        location.ResolvedAtUtc.ShouldBe(LocationTestFactory.ResolvedAt);
        location.IsUsable.ShouldBeTrue();
        location.Coordinates.Latitude.ShouldBe(6.4512m);
        location.Coordinates.Longitude.ShouldBe(7.5221m);

        var reference = location.ProviderReferences.ShouldHaveSingleItem();
        reference.LocationId.ShouldBe(location.Id);
        reference.Provider.ShouldBe("mapbox");
        reference.FeatureId.ShouldBe("dXJuOm1ieGFkcjox");
        reference.FeatureType.ShouldBe("address");
        reference.StorageClassification.ShouldBe(LocationStorageClassification.PermanentGeocoding);
        location.HasProviderReference("MAPBOX", "dXJuOm1ieGFkcjox").ShouldBeTrue();
    }
}
