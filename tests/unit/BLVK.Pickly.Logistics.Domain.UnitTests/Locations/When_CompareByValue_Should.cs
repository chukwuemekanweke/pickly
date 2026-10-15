using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_CreatingGeographicCoordinates_Should
{
[Fact]
    public void CompareByValue()
    {
        GeographicCoordinates.Create(6.45m, 7.52m).ShouldBe(GeographicCoordinates.Create(6.45m, 7.52m));
    }
}
