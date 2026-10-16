using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Theory]
    [InlineData("", "feature")]
    [InlineData("mapbox", "")]
    public void RejectIncompleteProviderReference(string provider, string featureId)
    {
        Should.Throw<ArgumentException>(() => Location.RegisterProviderResolved(
            LocationTestFactory.Address(),
            GeographicCoordinates.Create(6.45m, 7.52m),
            LocationVisibility.Private,
            LocationResolutionSource.Mapbox,
            provider,
            featureId,
            "address",
            LocationTestFactory.ResolvedAt));
    }
}
