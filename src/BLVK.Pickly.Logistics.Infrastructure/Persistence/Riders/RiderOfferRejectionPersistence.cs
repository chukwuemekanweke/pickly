using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Riders;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderOfferRejectionPersistence(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderOfferRejectionPersistence
{
    public async Task<RiderOperationResult> RejectAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var preview = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (preview is null)
        {
            return new(RiderOperationCode.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(RiderOperationCode.Forbidden);
        }

        var profile = resolved.Profile!;
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await support.AcquireAssignmentLocksAsync(preview.DeliveryId, profile.StakeholderId, cancellationToken);
        var offer = await dbContext.RiderDeliveryOffers.SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (offer is null)
        {
            return new(RiderOperationCode.NotFound);
        }

        if (offer.Status == RiderDeliveryOfferStatus.Rejected)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(RiderOperationCode.Success);
        }

        if (offer.Status != RiderDeliveryOfferStatus.Pending)
        {
            return new(offer.Status == RiderDeliveryOfferStatus.Expired
                ? RiderOperationCode.Expired
                : RiderOperationCode.Conflict);
        }

        if (utcNow >= offer.ExpiresAtUtc)
        {
            offer.Expire(utcNow);
        }
        else
        {
            offer.Reject(utcNow);
        }

        var session = await dbContext.DeliveryMatchingSessions
            .SingleOrDefaultAsync(item => item.Id == offer.MatchingSessionId, cancellationToken);
        if (session?.Status == DeliveryMatchingSessionStatus.Active)
        {
            session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (offer.Status == RiderDeliveryOfferStatus.Rejected)
        {
            RiderMatchingMetrics.RecordOfferRejected();
        }

        return new(offer.Status == RiderDeliveryOfferStatus.Expired
            ? RiderOperationCode.Expired
            : RiderOperationCode.Success);
    }
}
