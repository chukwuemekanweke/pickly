using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed partial class When_ConfiguringLocationPersistence_Should
{
[Fact]
    public void RejectStoredPointWithUnexpectedSrid()
    {
        var point = GeographicCoordinatesConverter.ToPoint(GeographicCoordinates.Create(6.45m, 7.52m));
        point.SRID = 3857;

        Should.Throw<InvalidOperationException>(() => GeographicCoordinatesConverter.FromPoint(point));
    }
}
