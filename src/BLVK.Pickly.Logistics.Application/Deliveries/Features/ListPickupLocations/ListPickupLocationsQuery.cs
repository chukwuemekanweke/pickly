using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;

public sealed record ListPickupLocationsQuery(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
