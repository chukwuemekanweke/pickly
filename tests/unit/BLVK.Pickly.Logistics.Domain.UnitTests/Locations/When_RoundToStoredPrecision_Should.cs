using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_CreatingGeographicCoordinates_Should
{
[Fact]
    public void RoundToStoredPrecision()
    {
        var coordinates = GeographicCoordinates.Create(6.451234567891m, 7.522198765432m);

        coordinates.Latitude.ShouldBe(6.4512346m);
        coordinates.Longitude.ShouldBe(7.5221988m);
    }
}
