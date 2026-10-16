using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;

namespace BLVK.Pickly.Logistics.Application.Locations;

/// <summary>
/// A recent permanent-mode provider answer for a resolution key that did not produce a registered location
/// (ambiguous or unresolved). Reusing it stops retries of the same request from triggering repeated paid calls.
/// </summary>
public sealed record CachedGeocodingOutcome(
    string Provider,
    LocationResolutionSource ResolutionSource,
    IReadOnlyList<GeocodingCandidate> Candidates);
