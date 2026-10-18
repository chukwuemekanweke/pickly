using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
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

        var now = timeProvider.GetUtcNow();
        await using var transaction = await transactionFactory.BeginAsync(
            RiderTransactionIsolation.ReadCommitted,
            cancellationToken);
        await consistencyLock.AcquireRiderAsync(resolved.Profile!.StakeholderId, cancellationToken);
        resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(SetRiderAvailabilityStatus.NotRider);
        }

        var profile = resolved.Profile!;
        if (!profile.IsApproved)
        {
            return new(SetRiderAvailabilityStatus.ProfileInactive);
        }

        var state = await persistence.LoadAsync(profile, cancellationToken);
        if (command.IsAvailable && (state.HasActiveDelivery || !state.IsVehicleCategoryActive))
        {
            return new(SetRiderAvailabilityStatus.Conflict);
        }

        var hasFreshLocation = state.Location is not null &&
            state.Location.CapturedAtUtc >= now.ToUniversalTime() - options.Value.LocationFreshness;
        if (command.IsAvailable && !hasFreshLocation)
        {
            return new(SetRiderAvailabilityStatus.StaleLocation);
        }

        profile.SetAvailability(command.IsAvailable);
        if (!command.IsAvailable)
        {
            foreach (var offer in state.PendingOffers)
            {
                offer.Revoke(now);
                if (state.MatchingSessions.TryGetValue(offer.MatchingSessionId, out var session) &&
                    session.Status == DeliveryMatchingSessionStatus.Active)
                {
                    session.RecordAttempt(now, now + options.Value.RetryBackoff, session.SearchRadiusMeters);
                }
            }
        }

        await transaction.CommitAsync(cancellationToken);
        var status = await statusProjector.ProjectAsync(profile, now, options.Value, cancellationToken);
        return new(SetRiderAvailabilityStatus.Success, status);
    }
}
