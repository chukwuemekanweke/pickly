namespace BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

/// <summary>
/// A WGS84 (SRID 4326) coordinate pair. Persistence maps it to a PostGIS point where X is longitude and
/// Y is latitude; the named properties here exist so callers never have to remember that ordering.
/// </summary>
public sealed record GeographicCoordinates
{
    public const int Wgs84Srid = 4326;
    public const int StoredDecimalPlaces = 7;

    private GeographicCoordinates(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public decimal Latitude { get; }
    public decimal Longitude { get; }

    public static GeographicCoordinates Create(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees.");
        }

        if (longitude is < -180m or > 180m)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees.");
        }

        return new GeographicCoordinates(
            decimal.Round(latitude, StoredDecimalPlaces, MidpointRounding.AwayFromZero),
            decimal.Round(longitude, StoredDecimalPlaces, MidpointRounding.AwayFromZero));
    }

    public static GeographicCoordinates FromDegrees(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude))
        {
            throw new ArgumentException("Coordinates must be finite numbers.");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Coordinates are outside the WGS84 range.");
        }

        return Create((decimal)latitude, (decimal)longitude);
    }

    public static bool IsValid(decimal latitude, decimal longitude) =>
        latitude is >= -90m and <= 90m && longitude is >= -180m and <= 180m;
}
