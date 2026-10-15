using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed partial class When_ConfiguringLocationPersistence_Should
{
[Fact]
    public void ConvertCoordinatesWithLongitudeAsXAndLatitudeAsY()
    {
        var coordinates = GeographicCoordinates.Create(6.4512m, 7.5221m);

        var point = GeographicCoordinatesConverter.ToPoint(coordinates);

        point.X.ShouldBe(7.5221, 1e-9);
        point.Y.ShouldBe(6.4512, 1e-9);
        point.SRID.ShouldBe(4326);
        GeographicCoordinatesConverter.FromPoint(point).ShouldBe(coordinates);
    }
}
