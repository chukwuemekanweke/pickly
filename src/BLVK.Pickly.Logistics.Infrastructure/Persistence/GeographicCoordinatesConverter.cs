using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

/// <summary>
/// Maps the domain coordinate value object to a WGS84 PostGIS point. NetTopologySuite orders coordinates as
/// X = longitude, Y = latitude; this converter is the single place that ordering is applied.
/// </summary>
public sealed class GeographicCoordinatesConverter() : ValueConverter<GeographicCoordinates, Point>(
    coordinates => ToPoint(coordinates),
    point => FromPoint(point))
{
    public static Point ToPoint(GeographicCoordinates coordinates) =>
        new((double)coordinates.Longitude, (double)coordinates.Latitude) { SRID = GeographicCoordinates.Wgs84Srid };

    public static GeographicCoordinates FromPoint(Point point)
    {
        if (point.IsEmpty || point.SRID != GeographicCoordinates.Wgs84Srid)
        {
            throw new InvalidOperationException("Stored location coordinates must be a non-empty WGS84 point.");
        }

        return GeographicCoordinates.FromDegrees(point.Y, point.X);
    }
}
