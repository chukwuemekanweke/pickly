using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

public sealed record GeocodingResult(
    GeocodingStatus Status,
    IReadOnlyList<GeocodingCandidate> Candidates,
    TimeSpan? RetryAfter = null,
    string Provider = "",
    LocationResolutionSource ResolutionSource = default)
{
    public static GeocodingResult Failed(
        GeocodingStatus status,
        TimeSpan? retryAfter = null,
        string provider = "",
        LocationResolutionSource resolutionSource = default) =>
        new(status, [], retryAfter, provider, resolutionSource);
}
