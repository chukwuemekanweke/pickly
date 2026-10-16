using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;

public sealed class DeletePickupLocationHandler(
    IRepository<SavedPickupLocation> repository,
    IPickupLocationConsistencyLock consistencyLock,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<DeletePickupLocationResult> HandleAsync(
        DeletePickupLocationCommand command,
        CancellationToken cancellationToken)
    {
        var senderId = command.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new DeletePickupLocationResult(DeletePickupLocationStatus.NotAuthenticated);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await consistencyLock.AcquireAsync(senderId.Value, cancellationToken);

        var location = await repository.FirstOrDefaultAsync(
            new SavedPickupLocationByOwnerAndIdSpecification(
                senderId.Value,
                command.PickupLocationId,
                enableTracking: true),
            cancellationToken);

        if (location is null)
        {
            return new DeletePickupLocationResult(DeletePickupLocationStatus.NotFound);
        }

        var replaceDefault = location.IsDefault;
        location.SetDeletedAudit(timeProvider.GetUtcNow(), senderId.Value.ToString());
        repository.Update(location);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (replaceDefault)
        {
            var replacement = await repository.FirstOrDefaultAsync(
                new OldestPickupLocationByOwnerSpecification(senderId.Value, enableTracking: true),
                cancellationToken);
            if (replacement is not null)
            {
                replacement.SetAsDefault();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new DeletePickupLocationResult(DeletePickupLocationStatus.Success);
    }
}
