using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

/// <summary>
/// Forward geocoding under permanent-storage terms. Implementations must never return results obtained under
/// temporary-only storage conditions, because callers persist what they receive.
/// </summary>
public interface ILocationGeocodingProvider
{
    string ProviderName { get; }
    LocationResolutionSource ResolutionSource { get; }

    Task<GeocodingResult> ForwardGeocodePermanentAsync(
        GeocodingRequest request,
        CancellationToken cancellationToken);
}
