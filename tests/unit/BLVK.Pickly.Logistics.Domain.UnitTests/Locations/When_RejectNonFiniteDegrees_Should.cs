using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_CreatingGeographicCoordinates_Should
{
[Theory]
    [InlineData(double.NaN, 7.5)]
    [InlineData(6.4, double.PositiveInfinity)]
    public void RejectNonFiniteDegrees(double latitude, double longitude)
    {
        Should.Throw<ArgumentException>(() => GeographicCoordinates.FromDegrees(latitude, longitude));
    }
}
