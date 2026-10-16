using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;

/// <summary>
/// Associates the sender with an already-resolved shared location. This is a database-only operation: it never
/// geocodes, never calls an external provider and never creates a shared location implicitly.
/// </summary>
public sealed class CreatePickupLocationHandler(
    IRepository<SavedPickupLocation> repository,
    IRepository<Location> locations,
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

        var location = command.LocationId == Guid.Empty
            ? null
            : await locations.GetByIdAsync(command.LocationId, cancellationToken);
        if (location is null)
        {
            return new CreatePickupLocationResult(CreatePickupLocationStatus.LocationNotFound);
        }

        if (!location.IsUsable)
        {
            return new CreatePickupLocationResult(CreatePickupLocationStatus.LocationUnusable);
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

        var savedLocation = SavedPickupLocation.Create(
            senderId.Value,
            command.Label,
            location.Id,
            command.AdditionalDirections,
            shouldBecomeDefault);

        await repository.AddAsync(savedLocation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreatePickupLocationResult(
            CreatePickupLocationStatus.Success,
            PickupLocationResponse.FromEntities(savedLocation, location));
    }
}
