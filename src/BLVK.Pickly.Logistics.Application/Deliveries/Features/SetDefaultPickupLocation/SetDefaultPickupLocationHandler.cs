using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;

public sealed class SetDefaultPickupLocationHandler(
    IRepository<SavedPickupLocation> repository,
    IPickupLocationConsistencyLock consistencyLock,
    IUnitOfWork unitOfWork)
{
    public async Task<SetDefaultPickupLocationResult> HandleAsync(
        SetDefaultPickupLocationCommand command,
        CancellationToken cancellationToken)
    {
        var senderId = command.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new SetDefaultPickupLocationResult(SetDefaultPickupLocationStatus.NotAuthenticated);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await consistencyLock.AcquireAsync(senderId.Value, cancellationToken);

        var target = await repository.FirstOrDefaultAsync(
            new SavedPickupLocationByOwnerAndIdSpecification(
                senderId.Value,
                command.LocationId,
                enableTracking: true),
            cancellationToken);

        if (target is null)
        {
            return new SetDefaultPickupLocationResult(SetDefaultPickupLocationStatus.NotFound);
        }

        if (target.IsDefault)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SetDefaultPickupLocationResult(SetDefaultPickupLocationStatus.Success);
        }

        var currentDefault = await repository.FirstOrDefaultAsync(
            new DefaultPickupLocationByOwnerSpecification(senderId.Value, enableTracking: true),
            cancellationToken);

        if (currentDefault is not null)
        {
            currentDefault.ClearDefault();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        target.SetAsDefault();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SetDefaultPickupLocationResult(SetDefaultPickupLocationStatus.Success);
    }
}
