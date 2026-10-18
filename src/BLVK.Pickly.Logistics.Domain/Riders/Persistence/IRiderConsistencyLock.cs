namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderConsistencyLock
{
    Task AcquireRiderAsync(Guid riderStakeholderId, CancellationToken cancellationToken);
    Task AcquireAssignmentAsync(Guid deliveryId, Guid riderStakeholderId, CancellationToken cancellationToken);
    Task AcquireMatchingProcessorAsync(CancellationToken cancellationToken);
}
