using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Locations.Entities;

/// <summary>
/// A canonical, reusable physical location in the shared registry. It never carries sender-specific data
/// (labels, directions, contact details); those belong to the records that reference it.
/// </summary>
public sealed class Location : Entity, IAggregateRoot
{
    private readonly List<LocationProviderReference> _providerReferences = [];
    private readonly List<LocationResolutionKey> _resolutionKeys = [];

    private Location()
    {
    }

    private Location(
        LocationAddress address,
        GeographicCoordinates coordinates,
        LocationType locationType,
        LocationResolutionSource resolutionSource,
        LocationVisibility visibility,
        DateTimeOffset resolvedAtUtc)
    {
        Address = address;
        Coordinates = coordinates;
        LocationType = locationType;
        ResolutionSource = resolutionSource;
        ResolutionStatus = LocationResolutionStatus.Resolved;
        Visibility = visibility;
        ResolvedAtUtc = resolvedAtUtc;
    }

    public LocationAddress Address { get; private set; } = null!;
    public GeographicCoordinates Coordinates { get; private set; } = null!;
    public LocationType LocationType { get; private set; }
    public LocationResolutionSource ResolutionSource { get; private set; }
    public LocationResolutionStatus ResolutionStatus { get; private set; }
    public LocationVisibility Visibility { get; private set; }
    public DateTimeOffset ResolvedAtUtc { get; private set; }
    public IReadOnlyCollection<LocationProviderReference> ProviderReferences => _providerReferences;
    public IReadOnlyCollection<LocationResolutionKey> ResolutionKeys => _resolutionKeys;

    public bool IsUsable =>
        ResolutionStatus == LocationResolutionStatus.Resolved &&
        !IsDeleted &&
        LocationType is LocationType.Street or LocationType.Landmark;

    /// <summary>
    /// Registers a canonical street or landmark resolved through a permanent-storage geocoding request. Only results obtained under
    /// permanent storage terms may be persisted, so the provider reference is always classified that way.
    /// </summary>
    public static Location RegisterProviderResolved(
        LocationAddress address,
        GeographicCoordinates coordinates,
        LocationVisibility visibility,
        LocationResolutionSource resolutionSource,
        string provider,
        string featureId,
        string featureType,
        DateTimeOffset resolvedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(coordinates);

        var location = new Location(
            address,
            coordinates,
            string.Equals(featureType, "poi", StringComparison.OrdinalIgnoreCase)
                ? LocationType.Landmark
                : LocationType.Street,
            resolutionSource,
            visibility,
            resolvedAtUtc);
        location._providerReferences.Add(LocationProviderReference.Create(
            location.Id,
            provider,
            featureId,
            featureType,
            LocationStorageClassification.PermanentGeocoding,
            resolvedAtUtc));
        return location;
    }

    /// <summary>
    /// Registers a known landmark curated by Pickly. Landmarks are always publicly discoverable because they are
    /// offered to senders as explicit fallback pickup or drop-off points.
    /// </summary>
    public static Location RegisterLandmark(
        LocationAddress address,
        GeographicCoordinates coordinates,
        DateTimeOffset registeredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(coordinates);

        return new Location(
            address,
            coordinates,
            LocationType.Landmark,
            LocationResolutionSource.InternallyRegistered,
            LocationVisibility.Public,
            registeredAtUtc);
    }

    public bool HasProviderReference(string provider, string featureId) =>
        _providerReferences.Any(reference =>
            string.Equals(reference.Provider, LocationProviderReference.NormalizeProvider(provider), StringComparison.Ordinal) &&
            string.Equals(reference.FeatureId, featureId, StringComparison.Ordinal));

    /// <summary>
    /// Records that a confidently resolved request key identifies this location, so identical future requests are
    /// served from the registry. Returns false when the key is already recorded.
    /// </summary>
    public bool AddResolutionKey(string resolutionKey)
    {
        if (!IsUsable)
        {
            throw new AggregateStateException("A retired or deleted location cannot gain resolution keys.");
        }

        if (_resolutionKeys.Any(key => key.Key == resolutionKey))
        {
            return false;
        }

        _resolutionKeys.Add(LocationResolutionKey.Create(Id, resolutionKey));
        return true;
    }

    public void Retire() => ResolutionStatus = LocationResolutionStatus.Retired;
}
