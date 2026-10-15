namespace BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;

public interface IPickupLocationConsistencyLock
{
    Task AcquireAsync(Guid senderId, CancellationToken cancellationToken);
}
