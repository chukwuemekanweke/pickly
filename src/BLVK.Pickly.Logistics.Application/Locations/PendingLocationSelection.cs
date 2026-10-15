using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;

namespace BLVK.Pickly.Logistics.Application.Locations;

/// <summary>
/// Server-held candidates awaiting an explicit sender choice. Clients only ever receive opaque selection tokens, so
/// they cannot submit provider payloads or coordinates of their own.
/// </summary>
public sealed record PendingLocationSelection(
    Guid StakeholderId,
    string Provider,
    LocationResolutionSource ResolutionSource,
    IReadOnlyList<GeocodingCandidate> Candidates);
