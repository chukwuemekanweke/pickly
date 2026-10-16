using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;

/// <summary>
/// Re-points a sender's saved pickup location at another already-resolved shared location and updates its
/// sender-private metadata. The shared location itself is never modified, and no geocoding happens here.
/// </summary>
public sealed class UpdatePickupLocationHandler(
    IRepository<SavedPickupLocation> repository,
    IRepository<Location> locations,
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

        var savedLocation = await repository.FirstOrDefaultAsync(
            new SavedPickupLocationByOwnerAndIdSpecification(
                senderId.Value,
                command.PickupLocationId,
                enableTracking: true),
            cancellationToken);

        if (savedLocation is null)
        {
            return new UpdatePickupLocationResult(UpdatePickupLocationStatus.NotFound);
        }

        var location = command.LocationId == Guid.Empty
            ? null
            : await locations.GetByIdAsync(command.LocationId, cancellationToken);
        if (location is null)
        {
            return new UpdatePickupLocationResult(UpdatePickupLocationStatus.LocationNotFound);
        }

        if (!location.IsUsable)
        {
            return new UpdatePickupLocationResult(UpdatePickupLocationStatus.LocationUnusable);
        }

        savedLocation.Update(command.Label, location.Id, command.AdditionalDirections);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdatePickupLocationResult(UpdatePickupLocationStatus.Success);
    }
}
