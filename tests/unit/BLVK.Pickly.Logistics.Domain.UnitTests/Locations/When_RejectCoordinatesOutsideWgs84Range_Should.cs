using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_CreatingGeographicCoordinates_Should
{
[Theory]
    [InlineData(90.0001, 0)]
    [InlineData(-90.0001, 0)]
    [InlineData(0, 180.0001)]
    [InlineData(0, -180.0001)]
    public void RejectCoordinatesOutsideWgs84Range(double latitude, double longitude)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            GeographicCoordinates.Create((decimal)latitude, (decimal)longitude));
    }
}
