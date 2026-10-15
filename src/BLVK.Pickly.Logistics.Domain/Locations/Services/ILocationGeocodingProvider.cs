using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

public interface ILocationGeocodingProvider
{
    string ProviderName { get; }
    LocationResolutionSource ResolutionSource { get; }

    Task<GeocodingResult> ForwardGeocodeAsync(
        GeocodingRequest request,
        CancellationToken cancellationToken);
}
