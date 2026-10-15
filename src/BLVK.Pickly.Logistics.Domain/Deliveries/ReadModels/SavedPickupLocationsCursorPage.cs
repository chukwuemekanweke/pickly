namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public sealed record SavedPickupLocationsCursorPage(
    IReadOnlyList<SavedPickupLocationReadModel> Locations,
    bool HasMore);
