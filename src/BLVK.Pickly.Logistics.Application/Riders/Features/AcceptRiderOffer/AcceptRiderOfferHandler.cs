using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders.Specifications;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public sealed class AcceptRiderOfferHandler(
    RiderActorResolver actorResolver,
    IRepository<RiderDeliveryOffer> offers,
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderOfferAcceptancePersistence persistence,
    IRiderMatchingTelemetry telemetry,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<AcceptRiderOfferResult> HandleAsync(
        AcceptRiderOfferCommand command,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? AcceptRiderOfferStatus.NotAuthenticated
                : AcceptRiderOfferStatus.NotRider);
        }

        var preview = await offers.FirstOrDefaultAsync(
            new RiderDeliveryOfferByIdSpecification(command.OfferId),
            cancellationToken);
        if (preview is null)
        {
            return new(AcceptRiderOfferStatus.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(AcceptRiderOfferStatus.Forbidden);
        }

        try
        {
            return await transactionFactory.ExecuteAsync(
                // Read committed: the advisory locks serialize rider operations and LoadAsync row-locks the Delivery
                // against writers outside them, so every read after locking is current and contention needs no retry.
                RiderTransactionIsolation.ReadCommitted,
                new(nameof(AcceptRiderOfferHandler), command.OfferId),
                async (transaction, token) =>
                {
                    await consistencyLock.AcquireAssignmentAsync(
                        preview.DeliveryId,
                        resolved.Profile.StakeholderId,
                        token);
                    var tracked = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, token);
                    if (tracked.Status != RiderActorResolutionStatus.Success)
                    {
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.NotRider);
                    }

                    var profile = tracked.Profile!;
                    var state = await persistence.LoadAsync(command.OfferId, profile.Id, token);
                    if (state.Offer is null || state.Delivery is null || state.MatchingSession is null)
                    {
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.NotFound);
                    }

                    if (state.Offer.RiderProfileId != profile.Id)
                    {
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.Forbidden);
                    }

                    if (state.Offer.Status == RiderDeliveryOfferStatus.Accepted &&
                        state.Delivery.RiderId == profile.StakeholderId)
                    {
                        await transaction.CommitAsync(token);
                        return new AcceptRiderOfferResult(
                            AcceptRiderOfferStatus.Success,
                            RiderDeliverySnapshotFactory.Create(state.Delivery));
                    }

                    if (state.Offer.Status != RiderDeliveryOfferStatus.Pending)
                    {
                        return new AcceptRiderOfferResult(state.Offer.Status == RiderDeliveryOfferStatus.Expired
                            ? AcceptRiderOfferStatus.Expired
                            : AcceptRiderOfferStatus.Conflict);
                    }

                    var now = timeProvider.GetUtcNow();
                    if (now >= state.Offer.ExpiresAtUtc)
                    {
                        RiderOfferRelease.Expire(state.Offer, state.MatchingSession, now, options.Value);
                        await transaction.CommitAsync(token);
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.Expired);
                    }

                    var geography = await persistence.GetGeographicEligibilityAsync(
                        profile,
                        state.Delivery,
                        state.Location,
                        state.MatchingSession.SearchRadiusMeters,
                        now,
                        token);
                    var hasFreshLocation = state.Location is not null &&
                        state.Location.CapturedAtUtc >= now.ToUniversalTime() - options.Value.LocationFreshness;
                    var isEligible = profile.IsApproved &&
                        profile.HasEnabledAvailability &&
                        hasFreshLocation &&
                        profile.VehicleCategoryId == state.Delivery.VehicleCategoryId &&
                        await persistence.IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, token) &&
                        geography.IsEligible &&
                        state.Delivery.IsMatchingEligible &&
                        !await persistence.HasActiveDeliveryAsync(profile.StakeholderId, token);
                    if (!isEligible)
                    {
                        telemetry.RecordAssignmentConflict();
                        return new AcceptRiderOfferResult(AcceptRiderOfferStatus.Conflict);
                    }

                    state.Offer.Accept(now);
                    state.Delivery.AssignRider(
                        profile.StakeholderId,
                        now,
                        profile.StakeholderId.ToString(),
                        "rider_offer_acceptance");
                    persistence.AddStatusHistory(state.Delivery.StatusHistory.Last());
                    state.MatchingSession.Complete(now);

                    var conflictingOffers = await persistence.ListConflictingOffersAsync(
                        state.Offer.Id,
                        state.Delivery.Id,
                        profile.Id,
                        token);
                    foreach (var conflictingOffer in conflictingOffers)
                    {
                        conflictingOffer.Revoke(now);
                    }

                    await transaction.CommitAsync(token);
                    telemetry.RecordOfferAccepted();
                    return new AcceptRiderOfferResult(
                        AcceptRiderOfferStatus.Success,
                        RiderDeliverySnapshotFactory.Create(state.Delivery));
                },
                cancellationToken);
        }
        catch (AggregateStateException)
        {
            telemetry.RecordAssignmentConflict();
            return new(AcceptRiderOfferStatus.Conflict);
        }
        catch (RiderPersistenceConflictException)
        {
            telemetry.RecordAssignmentConflict();
            return new(AcceptRiderOfferStatus.Conflict);
        }
        catch (RiderTransientPersistenceException)
        {
            return new(AcceptRiderOfferStatus.TemporarilyUnavailable);
        }
        catch (RiderCommitOutcomeUnknownException)
        {
            var acceptedDelivery = await persistence.FindAcceptedDeliveryAsync(
                command.OfferId,
                resolved.Profile.Id,
                resolved.Profile.StakeholderId,
                cancellationToken);
            if (acceptedDelivery is null)
            {
                throw;
            }

            telemetry.RecordOfferAccepted();
            return new(
                AcceptRiderOfferStatus.Success,
                RiderDeliverySnapshotFactory.Create(acceptedDelivery));
        }
    }
}
