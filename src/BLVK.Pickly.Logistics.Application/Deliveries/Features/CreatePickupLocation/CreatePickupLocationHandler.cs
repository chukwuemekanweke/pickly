using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;

public sealed class CreatePickupLocationHandler(
    IRepository<SavedPickupLocation> repository,
    IPickupLocationConsistencyLock consistencyLock,
    IUnitOfWork unitOfWork)
{
    public async Task<CreatePickupLocationResult> HandleAsync(
        CreatePickupLocationCommand command,
        CancellationToken cancellationToken)
    {
        var senderId = command.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new CreatePickupLocationResult(CreatePickupLocationStatus.NotAuthenticated);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await consistencyLock.AcquireAsync(senderId.Value, cancellationToken);

        var currentDefault = await repository.FirstOrDefaultAsync(
            new DefaultPickupLocationByOwnerSpecification(senderId.Value, enableTracking: true),
            cancellationToken);

        var shouldBecomeDefault = currentDefault is null || command.IsDefault;
        if (command.IsDefault && currentDefault is not null)
        {
            currentDefault.ClearDefault();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var location = SavedPickupLocation.Create(
            senderId.Value,
            command.Label,
            command.Address.ToDomain(),
            shouldBecomeDefault);

        await repository.AddAsync(location, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreatePickupLocationResult(
            CreatePickupLocationStatus.Success,
            PickupLocationResponse.FromEntity(location));
    }
}
