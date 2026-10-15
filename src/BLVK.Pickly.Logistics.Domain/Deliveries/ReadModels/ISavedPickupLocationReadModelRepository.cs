namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public interface ISavedPickupLocationReadModelRepository
{
    Task<SavedPickupLocationReadModel?> GetByIdAsync(
        Guid senderId,
        Guid locationId,
        CancellationToken cancellationToken);

    Task<SavedPickupLocationReadModel?> GetDefaultAsync(
        Guid senderId,
        CancellationToken cancellationToken);

    Task<SavedPickupLocationsCursorPage> ListAsync(
        SavedPickupLocationsCursorRequest request,
        CancellationToken cancellationToken);
}
