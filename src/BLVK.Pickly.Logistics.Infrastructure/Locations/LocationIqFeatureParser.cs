using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using System.Globalization;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

internal static class LocationIqFeatureParser
{
    private static readonly HashSet<string> LandmarkClasses = ["amenity", "historic", "leisure", "office", "shop", "tourism"];

    public static GeocodingResult Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return GeocodingResult.Failed(GeocodingStatus.InvalidResponse);
        }

        var candidates = root.EnumerateArray().Select(TryParse).Where(candidate => candidate is not null).ToArray();
        return root.GetArrayLength() > 0 && candidates.Length == 0
            ? GeocodingResult.Failed(GeocodingStatus.InvalidResponse)
            : new GeocodingResult(GeocodingStatus.Success, candidates!);
    }

    internal static GeocodingPrecision MapPrecision(string featureClass, string featureType) =>
        featureClass == "highway" || featureType is "road" or "residential" or "street"
            ? GeocodingPrecision.Street
            : LandmarkClasses.Contains(featureClass)
                ? GeocodingPrecision.Landmark
                : featureClass == "building" || featureType is "house" or "building"
                    ? GeocodingPrecision.Premise
                    : featureClass == "place" || featureType is "postcode" or "administrative"
                        ? GeocodingPrecision.Area
                        : GeocodingPrecision.Region;

    private static GeocodingCandidate? TryParse(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object ||
            !TryDecimalString(item, "lat", out var latitude) ||
            !TryDecimalString(item, "lon", out var longitude) ||
            !GeographicCoordinates.IsValid(latitude, longitude))
        {
            return null;
        }

        var placeId = GetString(item, "place_id");
        var osmType = GetString(item, "osm_type");
        var osmId = GetString(item, "osm_id");
        var featureId = osmType is not null && osmId is not null ? $"{osmType.ToLowerInvariant()}/{osmId}" : placeId;
        var featureClass = GetString(item, "class")?.ToLowerInvariant();
        var featureType = GetString(item, "type")?.ToLowerInvariant();
        var displayAddress = GetString(item, "display_name");
        if (featureId is null || featureClass is null || featureType is null || displayAddress is null ||
            !item.TryGetProperty("address", out var address) || address.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var precision = MapPrecision(featureClass, featureType);
        var addressLine = precision switch
        {
            GeocodingPrecision.Street => GetString(address, "road"),
            GeocodingPrecision.Landmark => GetString(item, "name") ?? GetString(address, "name") ?? GetString(address, featureType),
            _ => GetString(address, "road") ?? GetString(item, "name")
        };
        var countryCode = GetString(address, "country_code")?.ToUpper(CultureInfo.InvariantCulture);
        if (addressLine is null || countryCode is null)
        {
            return null;
        }

        return new GeocodingCandidate(
            featureId,
            $"{featureClass}:{featureType}",
            precision,
            GeocodingMatchConfidence.Unknown,
            displayAddress,
            addressLine,
            GetString(address, "city") ?? GetString(address, "town") ?? GetString(address, "village") ?? GetString(address, "municipality") ?? string.Empty,
            GetString(address, "state") ?? string.Empty,
            countryCode,
            latitude,
            longitude);
    }

    private static bool TryDecimalString(JsonElement element, string name, out decimal value)
    {
        value = 0;
        return element.TryGetProperty(name, out var node) &&
               ((node.ValueKind == JsonValueKind.String && decimal.TryParse(node.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) ||
                (node.ValueKind == JsonValueKind.Number && node.TryGetDecimal(out value)));
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.String when !string.IsNullOrWhiteSpace(node.GetString()) => node.GetString()!.Trim(),
            JsonValueKind.Number => node.GetRawText(),
            _ => null
        };
    }
}
