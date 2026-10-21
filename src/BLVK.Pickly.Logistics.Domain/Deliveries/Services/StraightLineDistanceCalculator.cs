using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Services;

public static class StraightLineDistanceCalculator
{
    private const double MeanEarthRadiusMeters = 6_371_008.8;

    public static long CalculateMeters(GeographicCoordinates origin, GeographicCoordinates destination)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        var originLatitude = ToRadians(origin.Latitude);
        var destinationLatitude = ToRadians(destination.Latitude);
        var latitudeDelta = destinationLatitude - originLatitude;
        var longitudeDelta = ToRadians(destination.Longitude - origin.Longitude);
        var haversine =
            Math.Pow(Math.Sin(latitudeDelta / 2d), 2d) +
            Math.Cos(originLatitude) * Math.Cos(destinationLatitude) *
            Math.Pow(Math.Sin(longitudeDelta / 2d), 2d);
        var angularDistance = 2d * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0d, 1d)));

        return checked((long)Math.Round(
            MeanEarthRadiusMeters * angularDistance,
            MidpointRounding.AwayFromZero));
    }

    private static double ToRadians(decimal degrees) =>
        (double)degrees * Math.PI / 180d;
}
