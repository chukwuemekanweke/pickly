using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;

public sealed class RejectRiderOfferHandler(
    RiderActorResolver actorResolver,
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderOfferRejectionPersistence persistence,
    IRiderMatchingTelemetry telemetry,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<RejectRiderOfferResult> HandleAsync(
        RejectRiderOfferCommand command,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? RejectRiderOfferStatus.NotAuthenticated
                : RejectRiderOfferStatus.NotRider);
        }

        var preview = await persistence.FindPreviewAsync(command.OfferId, cancellationToken);
        if (preview is null)
        {
            return new(RejectRiderOfferStatus.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(RejectRiderOfferStatus.Forbidden);
        }

        await using var transaction = await transactionFactory.BeginAsync(
            RiderTransactionIsolation.ReadCommitted,
            cancellationToken);
        await consistencyLock.AcquireAssignmentAsync(
            preview.DeliveryId,
            resolved.Profile.StakeholderId,
            cancellationToken);
        var state = await persistence.LoadAsync(command.OfferId, cancellationToken);
        if (state.Offer is null)
        {
            return new(RejectRiderOfferStatus.NotFound);
        }

        if (state.Offer.RiderProfileId != resolved.Profile.Id)
        {
            return new(RejectRiderOfferStatus.Forbidden);
        }

        if (state.Offer.Status == RiderDeliveryOfferStatus.Rejected)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(RejectRiderOfferStatus.Success);
        }

        if (state.Offer.Status != RiderDeliveryOfferStatus.Pending)
        {
            return new(state.Offer.Status == RiderDeliveryOfferStatus.Expired
                ? RejectRiderOfferStatus.Expired
                : RejectRiderOfferStatus.Conflict);
        }

        var now = timeProvider.GetUtcNow();
        if (now >= state.Offer.ExpiresAtUtc)
        {
            state.Offer.Expire(now);
        }
        else
        {
            state.Offer.Reject(now);
        }

        if (state.MatchingSession?.Status == DeliveryMatchingSessionStatus.Active)
        {
            state.MatchingSession.RecordAttempt(
                now,
                now + options.Value.RetryBackoff,
                state.MatchingSession.SearchRadiusMeters);
        }

        await transaction.CommitAsync(cancellationToken);
        if (state.Offer.Status == RiderDeliveryOfferStatus.Rejected)
        {
            telemetry.RecordOfferRejected();
            return new(RejectRiderOfferStatus.Success);
        }

        return new(RejectRiderOfferStatus.Expired);
    }
}
