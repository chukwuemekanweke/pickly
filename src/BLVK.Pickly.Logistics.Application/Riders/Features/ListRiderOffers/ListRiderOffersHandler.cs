using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;

public sealed class ListRiderOffersHandler(
    RiderActorResolver actorResolver,
    IRiderOfferReader reader,
    TimeProvider timeProvider)
{
    public async Task<ListRiderOffersResult> HandleAsync(
        ListRiderOffersQuery query,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(query.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? ListRiderOffersStatus.NotAuthenticated
                : ListRiderOffersStatus.NotRider);
        }

        if (!resolved.Profile!.IsApproved)
        {
            return new(ListRiderOffersStatus.ProfileInactive);
        }

        // Read-only: timed-out offers are filtered here and transitioned (with session backoff) by the Jobs expiry
        // step, or by acceptance/rejection when the rider acts on one first.
        var offers = await reader.ListActiveSnapshotsAsync(resolved.Profile.Id, timeProvider.GetUtcNow(), cancellationToken);
        return new(ListRiderOffersStatus.Success, offers);
    }
}
