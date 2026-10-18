using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;

public sealed class ListRiderOffersHandler(
    RiderActorResolver actorResolver,
    IRiderOfferReader reader,
    IRiderTransactionFactory transactionFactory,
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

        var now = timeProvider.GetUtcNow();
        await using var transaction = await transactionFactory.BeginAsync(
            RiderTransactionIsolation.ReadCommitted,
            cancellationToken);
        var expired = await reader.ListExpiredAsync(resolved.Profile.Id, now, cancellationToken);
        foreach (var offer in expired)
        {
            offer.Expire(now);
        }

        await transaction.CommitAsync(cancellationToken);
        var offers = await reader.ListActiveSnapshotsAsync(resolved.Profile.Id, now, cancellationToken);
        return new(ListRiderOffersStatus.Success, offers);
    }
}
