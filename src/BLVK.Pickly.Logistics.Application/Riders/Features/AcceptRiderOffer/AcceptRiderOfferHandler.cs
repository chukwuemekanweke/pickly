using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public sealed class AcceptRiderOfferHandler(
    RiderActorResolver actorResolver,
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

        var preview = await persistence.FindPreviewAsync(command.OfferId, cancellationToken);
        if (preview is null)
        {
            return new(AcceptRiderOfferStatus.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(AcceptRiderOfferStatus.Forbidden);
        }

        await using var transaction = await transactionFactory.BeginAsync(
            RiderTransactionIsolation.Serializable,
            cancellationToken);
        try
        {
            await consistencyLock.AcquireAssignmentAsync(
                preview.DeliveryId,
                resolved.Profile.StakeholderId,
                cancellationToken);
            resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, cancellationToken);
            if (resolved.Status != RiderActorResolutionStatus.Success)
            {
                return new(AcceptRiderOfferStatus.NotRider);
            }

            var profile = resolved.Profile!;
            var state = await persistence.LoadAsync(command.OfferId, profile.Id, cancellationToken);
            if (state.Offer is null || state.Delivery is null || state.MatchingSession is null)
            {
                return new(AcceptRiderOfferStatus.NotFound);
            }

            if (state.Offer.RiderProfileId != profile.Id)
            {
                return new(AcceptRiderOfferStatus.Forbidden);
            }

            if (state.Offer.Status == RiderDeliveryOfferStatus.Accepted &&
                state.Delivery.RiderId == profile.StakeholderId)
            {
                await transaction.CommitAsync(cancellationToken);
                return new(
                    AcceptRiderOfferStatus.Success,
                    RiderDeliverySnapshotFactory.Create(state.Delivery));
            }

            if (state.Offer.Status != RiderDeliveryOfferStatus.Pending)
            {
                return new(state.Offer.Status == RiderDeliveryOfferStatus.Expired
                    ? AcceptRiderOfferStatus.Expired
                    : AcceptRiderOfferStatus.Conflict);
            }

            var now = timeProvider.GetUtcNow();
            if (now >= state.Offer.ExpiresAtUtc)
            {
                state.Offer.Expire(now);
                await transaction.CommitAsync(cancellationToken);
                return new(AcceptRiderOfferStatus.Expired);
            }

            var geography = await persistence.GetGeographicEligibilityAsync(
                profile,
                state.Delivery,
                state.Location,
                state.MatchingSession.SearchRadiusMeters,
                now,
                cancellationToken);
            var hasFreshLocation = state.Location is not null &&
                state.Location.CapturedAtUtc >= now.ToUniversalTime() - options.Value.LocationFreshness;
            var isEligible = profile.IsApproved &&
                profile.HasEnabledAvailability &&
                hasFreshLocation &&
                profile.VehicleCategoryId == state.Delivery.VehicleCategoryId &&
                await persistence.IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, cancellationToken) &&
                geography.IsEligible &&
                state.Delivery.IsMatchingEligible &&
                !await persistence.HasActiveDeliveryAsync(profile.StakeholderId, cancellationToken);
            if (!isEligible)
            {
                telemetry.RecordAssignmentConflict();
                return new(AcceptRiderOfferStatus.Conflict);
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
                cancellationToken);
            foreach (var conflictingOffer in conflictingOffers)
            {
                conflictingOffer.Revoke(now);
            }

            await transaction.CommitAsync(cancellationToken);
            telemetry.RecordOfferAccepted();
            return new(
                AcceptRiderOfferStatus.Success,
                RiderDeliverySnapshotFactory.Create(state.Delivery));
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
    }
}
