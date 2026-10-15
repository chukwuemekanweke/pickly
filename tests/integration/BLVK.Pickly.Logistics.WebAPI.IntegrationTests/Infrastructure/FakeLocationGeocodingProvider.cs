using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the Mapbox client in integration tests so no paid provider request is ever sent. Unless a scenario
/// configures a responder, any geocoding attempt fails the request, which makes accidental geocoding visible.
/// </summary>
public sealed class FakeLocationGeocodingProvider : ILocationGeocodingProvider
{
    private int _callCount;

    public string ProviderName => "mapbox";
    public LocationResolutionSource ResolutionSource => LocationResolutionSource.Mapbox;

    public int CallCount => Volatile.Read(ref _callCount);

    public Func<GeocodingRequest, CancellationToken, Task<GeocodingResult>>? Responder { get; set; }

    public async Task<GeocodingResult> ForwardGeocodePermanentAsync(
        GeocodingRequest request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        if (Responder is null)
        {
            throw new InvalidOperationException("Geocoding was not expected in this scenario.");
        }

        return await Responder(request, cancellationToken);
    }
}
