namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed record ResolveLocationResult(
    ResolveLocationStatus Status,
    LocationResponse? Location = null,
    IReadOnlyList<LocationCandidateResponse>? Candidates = null,
    IReadOnlyList<NearbyLocationResponse>? NearbyLandmarks = null,
    bool FromRegistry = false,
    TimeSpan? RetryAfter = null);
