namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

/// <summary>
/// Transaction-scoped rider/delivery locks. Every caller acquires delivery locks (ascending) before the rider lock,
/// and never acquires a delivery lock after a rider lock.
/// </summary>
public interface IRiderConsistencyLock
{
    Task AcquireRiderAsync(Guid riderStakeholderId, CancellationToken cancellationToken);
    Task AcquireDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task<bool> TryAcquireDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task AcquireAssignmentAsync(Guid deliveryId, Guid riderStakeholderId, CancellationToken cancellationToken);

    Task AcquireDeliveriesThenRiderAsync(
        IReadOnlyCollection<Guid> deliveryIds,
        Guid riderStakeholderId,
        CancellationToken cancellationToken);
}
