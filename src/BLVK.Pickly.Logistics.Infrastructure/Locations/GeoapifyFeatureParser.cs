using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using System.Globalization;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

internal static class GeoapifyFeatureParser
{
    public static GeocodingResult Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array)
        {
            return GeocodingResult.Failed(GeocodingStatus.InvalidResponse);
        }

        var candidates = features.EnumerateArray().Select(TryParse).Where(candidate => candidate is not null).ToArray();
        return features.GetArrayLength() > 0 && candidates.Length == 0
            ? GeocodingResult.Failed(GeocodingStatus.InvalidResponse)
            : new GeocodingResult(GeocodingStatus.Success, candidates!);
    }

    internal static GeocodingPrecision MapPrecision(string resultType) => resultType switch
    {
        "street" => GeocodingPrecision.Street,
        "amenity" => GeocodingPrecision.Landmark,
        "building" => GeocodingPrecision.Premise,
        "suburb" or "district" or "postcode" or "city" or "county" => GeocodingPrecision.Area,
        _ => GeocodingPrecision.Region
    };

    internal static GeocodingMatchConfidence MapConfidence(decimal? confidence) => confidence switch
    {
        >= 0.99m => GeocodingMatchConfidence.Exact,
        >= 0.90m => GeocodingMatchConfidence.High,
        >= 0.50m => GeocodingMatchConfidence.Medium,
        > 0 => GeocodingMatchConfidence.Low,
        _ => GeocodingMatchConfidence.Unknown
    };

    private static GeocodingCandidate? TryParse(JsonElement feature)
    {
        if (feature.ValueKind != JsonValueKind.Object ||
            !feature.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object ||
            !TryCoordinates(feature, out var latitude, out var longitude))
        {
            return null;
        }

        var featureId = GetString(properties, "place_id");
        var resultType = GetString(properties, "result_type");
        var displayAddress = GetString(properties, "formatted");
        var countryCode = GetString(properties, "country_code")?.ToUpper(CultureInfo.InvariantCulture);
        if (featureId is null || resultType is null || displayAddress is null || countryCode is null)
        {
            return null;
        }

        var precision = MapPrecision(resultType);
        var addressLine = precision switch
        {
            GeocodingPrecision.Street => GetString(properties, "street"),
            GeocodingPrecision.Landmark => GetString(properties, "name") ?? GetString(properties, "address_line1"),
            _ => GetString(properties, "address_line1") ?? GetString(properties, "name")
        };
        if (string.IsNullOrWhiteSpace(addressLine))
        {
            return null;
        }

        decimal? confidence = null;
        if (properties.TryGetProperty("rank", out var rank) &&
            rank.ValueKind == JsonValueKind.Object &&
            rank.TryGetProperty("confidence", out var confidenceNode) &&
            confidenceNode.TryGetDecimal(out var value))
        {
            confidence = value;
        }

        return new GeocodingCandidate(
            featureId,
            resultType,
            precision,
            MapConfidence(confidence),
            displayAddress,
            addressLine,
            GetString(properties, "city") ?? GetString(properties, "town") ?? GetString(properties, "village") ?? string.Empty,
            GetString(properties, "state") ?? string.Empty,
            countryCode,
            latitude,
            longitude);
    }

    private static bool TryCoordinates(JsonElement feature, out decimal latitude, out decimal longitude)
    {
        latitude = 0;
        longitude = 0;
        return feature.TryGetProperty("geometry", out var geometry) &&
               geometry.ValueKind == JsonValueKind.Object &&
               geometry.TryGetProperty("coordinates", out var coordinates) &&
               coordinates.ValueKind == JsonValueKind.Array &&
               coordinates.GetArrayLength() >= 2 &&
               coordinates[0].TryGetDecimal(out longitude) &&
               coordinates[1].TryGetDecimal(out latitude) &&
               GeographicCoordinates.IsValid(latitude, longitude);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(node.GetString())
            ? node.GetString()!.Trim()
            : null;
}
