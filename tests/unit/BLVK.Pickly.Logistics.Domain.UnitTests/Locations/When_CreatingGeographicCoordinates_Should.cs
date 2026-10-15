using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_CreatingGeographicCoordinates_Should
{
    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    [InlineData(6.4512, 7.5221)]
    public void AcceptCoordinatesWithinWgs84Range(double latitude, double longitude)
    {
        var coordinates = GeographicCoordinates.Create((decimal)latitude, (decimal)longitude);

        coordinates.Latitude.ShouldBe((decimal)latitude);
        coordinates.Longitude.ShouldBe((decimal)longitude);
    }
}
