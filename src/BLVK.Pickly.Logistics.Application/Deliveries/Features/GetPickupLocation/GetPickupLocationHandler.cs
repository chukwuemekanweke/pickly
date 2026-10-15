using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;

public sealed class GetPickupLocationHandler(ISavedPickupLocationReadModelRepository repository)
{
    public async Task<GetPickupLocationResult> HandleAsync(
        GetPickupLocationQuery query,
        CancellationToken cancellationToken)
    {
        var senderId = query.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new GetPickupLocationResult(GetPickupLocationStatus.NotAuthenticated);
        }

        var location = await repository.GetByIdAsync(senderId.Value, query.PickupLocationId, cancellationToken);
        return location is null
            ? new GetPickupLocationResult(GetPickupLocationStatus.NotFound)
            : new GetPickupLocationResult(
                GetPickupLocationStatus.Success,
                PickupLocationResponse.FromReadModel(location));
    }
}
