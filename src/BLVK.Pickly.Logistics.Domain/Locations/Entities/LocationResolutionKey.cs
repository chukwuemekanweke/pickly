using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Locations.Entities;

/// <summary>
/// A hashed, normalized resolution request known to identify a location with confidence. Only the hash is stored,
/// so the registry can answer repeated requests without retaining the raw customer-entered text.
/// </summary>
public sealed class LocationResolutionKey : Entity
{
    private LocationResolutionKey()
    {
    }

    private LocationResolutionKey(Guid locationId, string key)
    {
        LocationId = locationId;
        Key = key;
    }

    public Guid LocationId { get; private set; }
    public string Key { get; private set; } = string.Empty;

    internal static LocationResolutionKey Create(Guid locationId, string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            key.Length != LocationAddress.ResolutionKeyLength ||
            !key.All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f'))
        {
            throw new ArgumentException("Resolution key must be a lowercase SHA-256 hex digest.", nameof(key));
        }

        return new LocationResolutionKey(locationId, key);
    }
}
