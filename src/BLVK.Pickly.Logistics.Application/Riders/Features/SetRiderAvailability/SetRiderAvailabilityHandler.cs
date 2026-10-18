using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;

public sealed class SetRiderAvailabilityHandler(
    RiderActorResolver actorResolver,
    RiderStatusProjector statusProjector,
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderAvailabilityPersistence persistence,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    private const int MaximumLockSetAttempts = 3;

    public async Task<SetRiderAvailabilityResult> HandleAsync(
        SetRiderAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? SetRiderAvailabilityStatus.NotAuthenticated
                : SetRiderAvailabilityStatus.NotRider);
        }

        // Revoking an offer also updates its delivery's matching session, so going offline must hold those
        // delivery locks, and delivery locks must precede the rider lock.
        IReadOnlyCollection<Guid> deliveryLocks = command.IsAvailable
            ? []
            : await persistence.ListPendingOfferDeliveryIdsAsync(resolved.Profile!.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();
        try
        {
            for (var attempt = 1; attempt <= MaximumLockSetAttempts; attempt++)
            {
                var outcome = await ExecuteAsync(command, resolved.Profile!.StakeholderId, deliveryLocks, now, cancellationToken);
                if (outcome.Result is not null)
                {
                    return outcome.Result;
                }

                deliveryLocks = outcome.RequiredDeliveryLocks;
            }

            return new(SetRiderAvailabilityStatus.TemporarilyUnavailable);
        }
        catch (RiderTransientPersistenceException)
        {
            return new(SetRiderAvailabilityStatus.TemporarilyUnavailable);
        }
    }

    private Task<AvailabilityAttempt> ExecuteAsync(
        SetRiderAvailabilityCommand command,
        Guid riderStakeholderId,
        IReadOnlyCollection<Guid> deliveryLocks,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(SetRiderAvailabilityHandler), riderStakeholderId),
            async (transaction, token) =>
            {
                await consistencyLock.AcquireDeliveriesThenRiderAsync(deliveryLocks, riderStakeholderId, token);
                var tracked = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, token);
                if (tracked.Status != RiderActorResolutionStatus.Success)
                {
                    return AvailabilityAttempt.Done(new(SetRiderAvailabilityStatus.NotRider));
                }

                var profile = tracked.Profile!;
                if (!profile.IsApproved)
                {
                    return AvailabilityAttempt.Done(new(SetRiderAvailabilityStatus.ProfileInactive));
                }

                var state = await persistence.LoadAsync(profile, token);
                if (command.IsAvailable && (state.HasActiveDelivery || !state.IsVehicleCategoryActive))
                {
                    return AvailabilityAttempt.Done(new(SetRiderAvailabilityStatus.Conflict));
                }

                var hasFreshLocation = state.Location is not null &&
                    state.Location.CapturedAtUtc >= now.ToUniversalTime() - options.Value.LocationFreshness;
                if (command.IsAvailable && !hasFreshLocation)
                {
                    return AvailabilityAttempt.Done(new(SetRiderAvailabilityStatus.StaleLocation));
                }

                if (!command.IsAvailable)
                {
                    // An offer created before the rider lock was taken belongs to a delivery that is not locked yet;
                    // roll back and retry with the complete lock set rather than lock out of order.
                    var pendingDeliveryIds = state.PendingOffers.Select(offer => offer.DeliveryId).ToHashSet();
                    if (!pendingDeliveryIds.IsSubsetOf(deliveryLocks))
                    {
                        return AvailabilityAttempt.Relock(pendingDeliveryIds.Union(deliveryLocks).ToArray());
                    }
                }

                profile.SetAvailability(command.IsAvailable);
                if (!command.IsAvailable)
                {
                    foreach (var offer in state.PendingOffers)
                    {
                        RiderOfferRelease.Revoke(
                            offer,
                            state.MatchingSessions.GetValueOrDefault(offer.MatchingSessionId),
                            now,
                            options.Value);
                    }
                }

                await transaction.CommitAsync(token);
                var status = await statusProjector.ProjectAsync(profile, now, options.Value, token);
                return AvailabilityAttempt.Done(new(SetRiderAvailabilityStatus.Success, status));
            },
            cancellationToken);

    private sealed record AvailabilityAttempt(
        SetRiderAvailabilityResult? Result,
        IReadOnlyCollection<Guid> RequiredDeliveryLocks)
    {
        public static AvailabilityAttempt Done(SetRiderAvailabilityResult result) => new(result, []);

        public static AvailabilityAttempt Relock(IReadOnlyCollection<Guid> deliveryIds) => new(null, deliveryIds);
    }
}
