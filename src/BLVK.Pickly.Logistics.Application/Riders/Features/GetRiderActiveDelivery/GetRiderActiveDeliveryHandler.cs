using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderActiveDelivery;

public sealed class GetRiderActiveDeliveryHandler(
    RiderActorResolver actorResolver,
    IRiderActiveDeliveryReader reader)
{
    public async Task<GetRiderActiveDeliveryResult> HandleAsync(
        GetRiderActiveDeliveryQuery query,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(query.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? GetRiderActiveDeliveryStatus.NotAuthenticated
                : GetRiderActiveDeliveryStatus.NotRider);
        }

        var delivery = await reader.GetAsync(resolved.Profile!.StakeholderId, cancellationToken);
        return delivery is null
            ? new(GetRiderActiveDeliveryStatus.NotFound)
            : new(GetRiderActiveDeliveryStatus.Success, delivery);
    }
}
