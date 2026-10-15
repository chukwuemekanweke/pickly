using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Fact]
    public void RegisterLandmarkAsPublicInternallyRegisteredLocation()
    {
        var landmark = LocationTestFactory.Landmark();

        landmark.LocationType.ShouldBe(LocationType.Landmark);
        landmark.ResolutionSource.ShouldBe(LocationResolutionSource.InternallyRegistered);
        landmark.Visibility.ShouldBe(LocationVisibility.Public);
        landmark.ProviderReferences.ShouldBeEmpty();
    }
}
