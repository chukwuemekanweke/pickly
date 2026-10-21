using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_CalculatingStraightLineDistance_Should
{
    [Fact]
    public void ReturnZeroForTheSameCoordinates()
    {
        var coordinates = GeographicCoordinates.FromDegrees(6.45, 7.51);

        StraightLineDistanceCalculator.CalculateMeters(coordinates, coordinates).ShouldBe(0);
    }

    [Fact]
    public void CalculateGreatCircleDistanceRoundedToTheNearestMeter()
    {
        var origin = GeographicCoordinates.FromDegrees(6.44, 7.51);
        var destination = GeographicCoordinates.FromDegrees(6.46, 7.53);

        StraightLineDistanceCalculator.CalculateMeters(origin, destination).ShouldBe(3135);
    }
}
