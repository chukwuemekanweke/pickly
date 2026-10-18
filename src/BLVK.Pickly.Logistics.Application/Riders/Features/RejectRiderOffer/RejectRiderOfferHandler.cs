using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders.Specifications;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;

public sealed class RejectRiderOfferHandler(
    RiderActorResolver actorResolver,
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRepository<RiderDeliveryOffer> offers,
    IRepository<DeliveryMatchingSession> sessions,
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

        var preview = await offers.FirstOrDefaultAsync(
            new RiderDeliveryOfferByIdSpecification(command.OfferId),
            cancellationToken);
        if (preview is null)
        {
            return new(RejectRiderOfferStatus.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(RejectRiderOfferStatus.Forbidden);
        }

        try
        {
            return await transactionFactory.ExecuteAsync(
                RiderTransactionIsolation.ReadCommitted,
                new(nameof(RejectRiderOfferHandler), command.OfferId),
                async (transaction, token) =>
                {
                    await consistencyLock.AcquireAssignmentAsync(
                        preview.DeliveryId,
                        resolved.Profile.StakeholderId,
                        token);
                    var offer = await offers.GetByIdAsync(command.OfferId, token);
                    if (offer is null)
                    {
                        return new RejectRiderOfferResult(RejectRiderOfferStatus.NotFound);
                    }

                    if (offer.RiderProfileId != resolved.Profile.Id)
                    {
                        return new RejectRiderOfferResult(RejectRiderOfferStatus.Forbidden);
                    }

                    if (offer.Status == RiderDeliveryOfferStatus.Rejected)
                    {
                        await transaction.CommitAsync(token);
                        return new RejectRiderOfferResult(RejectRiderOfferStatus.Success);
                    }

                    if (offer.Status != RiderDeliveryOfferStatus.Pending)
                    {
                        return new RejectRiderOfferResult(offer.Status == RiderDeliveryOfferStatus.Expired
                            ? RejectRiderOfferStatus.Expired
                            : RejectRiderOfferStatus.Conflict);
                    }

                    var now = timeProvider.GetUtcNow();
                    var session = await sessions.GetByIdAsync(offer.MatchingSessionId, token);
                    if (now >= offer.ExpiresAtUtc)
                    {
                        RiderOfferRelease.Expire(offer, session, now, options.Value);
                    }
                    else
                    {
                        RiderOfferRelease.Reject(offer, session, now, options.Value);
                    }

                    await transaction.CommitAsync(token);
                    if (offer.Status == RiderDeliveryOfferStatus.Rejected)
                    {
                        telemetry.RecordOfferRejected();
                        return new RejectRiderOfferResult(RejectRiderOfferStatus.Success);
                    }

                    return new RejectRiderOfferResult(RejectRiderOfferStatus.Expired);
                },
                cancellationToken);
        }
        catch (RiderTransientPersistenceException)
        {
            return new(RejectRiderOfferStatus.TemporarilyUnavailable);
        }
    }
}
