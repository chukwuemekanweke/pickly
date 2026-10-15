using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_CreatingGeographicCoordinates_Should
{
[Fact]
    public void RejectOverflowingDegreesBeforeDecimalConversion()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GeographicCoordinates.FromDegrees(1e30, 7.5));
    }
}
