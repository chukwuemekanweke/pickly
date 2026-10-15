namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public sealed record SavedPickupLocationsCursorRequest(
    Guid SenderId,
    DateTimeOffset? CursorCreatedAtUtc,
    Guid? CursorLocationId,
    int Limit);
