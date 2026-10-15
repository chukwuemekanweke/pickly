using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.Entities;

/// <summary>
/// Provenance linking a shared location to the provider feature it was resolved from. Provider identifiers stay
/// here so the core location model remains provider-neutral.
/// </summary>
public sealed class LocationProviderReference : Entity
{
    public const int MaxProviderLength = 50;
    public const int MaxFeatureIdLength = 300;
    public const int MaxFeatureTypeLength = 50;

    private LocationProviderReference()
    {
    }

    private LocationProviderReference(
        Guid locationId,
        string provider,
        string featureId,
        string featureType,
        LocationStorageClassification storageClassification,
        DateTimeOffset resolvedAtUtc)
    {
        LocationId = locationId;
        Provider = NormalizeProvider(NormalizeRequired(provider, nameof(provider), MaxProviderLength));
        FeatureId = NormalizeRequired(featureId, nameof(featureId), MaxFeatureIdLength);
        FeatureType = NormalizeRequired(featureType, nameof(featureType), MaxFeatureTypeLength).ToLowerInvariant();
        StorageClassification = storageClassification;
        ResolvedAtUtc = resolvedAtUtc;
    }

    public Guid LocationId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string FeatureId { get; private set; } = string.Empty;
    public string FeatureType { get; private set; } = string.Empty;
    public LocationStorageClassification StorageClassification { get; private set; }
    public DateTimeOffset ResolvedAtUtc { get; private set; }

    public static string NormalizeProvider(string provider) => provider.Trim().ToLowerInvariant();

    internal static LocationProviderReference Create(
        Guid locationId,
        string provider,
        string featureId,
        string featureType,
        LocationStorageClassification storageClassification,
        DateTimeOffset resolvedAtUtc) =>
        new(locationId, provider, featureId, featureType, storageClassification, resolvedAtUtc);

    private static string NormalizeRequired(string? value, string argumentName, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value is required and must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}
