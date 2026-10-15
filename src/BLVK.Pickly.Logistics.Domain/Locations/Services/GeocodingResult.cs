namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

public sealed record GeocodingResult(
    GeocodingStatus Status,
    IReadOnlyList<GeocodingCandidate> Candidates,
    TimeSpan? RetryAfter = null)
{
    public static GeocodingResult Failed(GeocodingStatus status, TimeSpan? retryAfter = null) =>
        new(status, [], retryAfter);
}
