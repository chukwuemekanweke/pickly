using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using System.Globalization;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

/// <summary>
/// Translates a Mapbox Geocoding v6 FeatureCollection into provider-neutral candidates. Mapbox response objects never
/// leave this class.
/// </summary>
internal static class MapboxFeatureParser
{
    public static GeocodingResult Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array)
        {
            return GeocodingResult.Failed(GeocodingStatus.InvalidResponse);
        }

        var candidates = new List<GeocodingCandidate>();
        var featureCount = 0;
        foreach (var feature in features.EnumerateArray())
        {
            featureCount++;
            var candidate = TryParseFeature(feature);
            if (candidate is not null)
            {
                candidates.Add(candidate);
            }
        }

        // Features that exist but none of which carry usable identity or coordinates mean the provider response
        // cannot be trusted, which is different from an honest "no match".
        return featureCount > 0 && candidates.Count == 0
            ? GeocodingResult.Failed(GeocodingStatus.InvalidResponse)
            : new GeocodingResult(GeocodingStatus.Success, candidates);
    }

    public static GeocodingPrecision MapPrecision(string featureType) => featureType switch
    {
        "address" or "secondary_address" or "block" => GeocodingPrecision.Premise,
        "street" => GeocodingPrecision.Street,
        "neighborhood" or "locality" or "place" or "district" or "postcode" => GeocodingPrecision.Area,
        _ => GeocodingPrecision.Region
    };

    public static GeocodingMatchConfidence MapConfidence(string? confidence) => confidence switch
    {
        "exact" => GeocodingMatchConfidence.Exact,
        "high" => GeocodingMatchConfidence.High,
        "medium" => GeocodingMatchConfidence.Medium,
        "low" => GeocodingMatchConfidence.Low,
        _ => GeocodingMatchConfidence.Unknown
    };

    private static GeocodingCandidate? TryParseFeature(JsonElement feature)
    {
        if (feature.ValueKind != JsonValueKind.Object ||
            !feature.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object ||
            !TryGetCoordinates(feature, out var latitude, out var longitude))
        {
            return null;
        }

        var featureId = GetString(properties, "mapbox_id") ?? GetString(feature, "id");
        var featureType = GetString(properties, "feature_type");
        if (string.IsNullOrWhiteSpace(featureId) || string.IsNullOrWhiteSpace(featureType))
        {
            return null;
        }

        var context = properties.TryGetProperty("context", out var contextElement) &&
                      contextElement.ValueKind == JsonValueKind.Object
            ? contextElement
            : default;
        var name = GetString(properties, "name") ?? string.Empty;
        var displayAddress = GetString(properties, "full_address")
                             ?? JoinNonEmpty(", ", GetString(properties, "name_preferred") ?? name, GetString(properties, "place_formatted"));

        return new GeocodingCandidate(
            featureId,
            featureType,
            MapPrecision(featureType),
            MapConfidence(GetNestedString(properties, "match_code", "confidence")),
            displayAddress,
            ResolveAddressLine(featureType, name, context),
            GetContextName(context, "place") ?? GetContextName(context, "locality") ?? GetContextName(context, "district")
                ?? (featureType == "place" ? name : string.Empty),
            GetContextName(context, "region") ?? (featureType == "region" ? name : string.Empty),
            GetNestedString(context, "country", "country_code")?.ToUpper(CultureInfo.InvariantCulture) ?? string.Empty,
            latitude,
            longitude);
    }

    private static string ResolveAddressLine(string featureType, string name, JsonElement context)
    {
        if (featureType is "address" or "secondary_address" or "block")
        {
            var addressName = GetContextName(context, "address");
            var composed = JoinNonEmpty(
                " ",
                GetNestedString(context, "address", "address_number"),
                GetNestedString(context, "address", "street_name"));
            return addressName ?? (string.IsNullOrWhiteSpace(composed) ? name : composed);
        }

        return name;
    }

    private static bool TryGetCoordinates(JsonElement feature, out decimal latitude, out decimal longitude)
    {
        latitude = 0;
        longitude = 0;

        // GeoJSON order is [longitude, latitude].
        if (!feature.TryGetProperty("geometry", out var geometry) ||
            geometry.ValueKind != JsonValueKind.Object ||
            !geometry.TryGetProperty("coordinates", out var coordinates) ||
            coordinates.ValueKind != JsonValueKind.Array ||
            coordinates.GetArrayLength() < 2 ||
            coordinates[0].ValueKind != JsonValueKind.Number ||
            coordinates[1].ValueKind != JsonValueKind.Number ||
            !coordinates[0].TryGetDecimal(out longitude) ||
            !coordinates[1].TryGetDecimal(out latitude))
        {
            return false;
        }

        return GeographicCoordinates.IsValid(latitude, longitude);
    }

    private static string? GetContextName(JsonElement context, string key) => GetNestedString(context, key, "name");

    private static string? GetNestedString(JsonElement element, string objectName, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(objectName, out var nested) &&
        nested.ValueKind == JsonValueKind.Object
            ? GetString(nested, propertyName)
            : null;

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()!.Trim()
            : null;

    private static string JoinNonEmpty(string separator, params string?[] values) =>
        string.Join(separator, values.Where(value => !string.IsNullOrWhiteSpace(value)));
}
