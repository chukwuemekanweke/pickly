using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;

public sealed class UpdatePickupLocationHandler(
    IRepository<SavedPickupLocation> repository,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdatePickupLocationResult> HandleAsync(
        UpdatePickupLocationCommand command,
        CancellationToken cancellationToken)
    {
        var senderId = command.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new UpdatePickupLocationResult(UpdatePickupLocationStatus.NotAuthenticated);
        }

        var location = await repository.FirstOrDefaultAsync(
            new SavedPickupLocationByOwnerAndIdSpecification(
                senderId.Value,
                command.LocationId,
                enableTracking: true),
            cancellationToken);

        if (location is null)
        {
            return new UpdatePickupLocationResult(UpdatePickupLocationStatus.NotFound);
        }

        location.Update(command.Label, command.Address.ToDomain());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdatePickupLocationResult(UpdatePickupLocationStatus.Success);
    }
}
