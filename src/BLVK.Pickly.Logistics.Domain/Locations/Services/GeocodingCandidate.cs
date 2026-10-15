namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

public sealed record GeocodingCandidate(
    string FeatureId,
    string FeatureType,
    GeocodingPrecision Precision,
    GeocodingMatchConfidence Confidence,
    string DisplayAddress,
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    decimal Latitude,
    decimal Longitude);
