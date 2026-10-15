using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDefaultPickupLocation;

public sealed class GetDefaultPickupLocationHandler(ISavedPickupLocationReadModelRepository repository)
{
    public async Task<GetDefaultPickupLocationResult> HandleAsync(
        GetDefaultPickupLocationQuery query,
        CancellationToken cancellationToken)
    {
        var senderId = query.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new GetDefaultPickupLocationResult(GetDefaultPickupLocationStatus.NotAuthenticated);
        }

        var location = await repository.GetDefaultAsync(senderId.Value, cancellationToken);
        return location is null
            ? new GetDefaultPickupLocationResult(GetDefaultPickupLocationStatus.NoDefault)
            : new GetDefaultPickupLocationResult(
                GetDefaultPickupLocationStatus.Success,
                PickupLocationResponse.FromReadModel(location));
    }
}
