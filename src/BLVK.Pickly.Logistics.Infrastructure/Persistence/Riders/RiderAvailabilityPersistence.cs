using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderAvailabilityPersistence(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderAvailabilityPersistence
{
    public async Task<RiderStatusResult> SetAsync(
        ActorContext actor,
        bool isAvailable,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var profile = resolved.Profile!;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await support.AcquireRiderLockAsync(profile.StakeholderId, cancellationToken);
        await dbContext.Entry(profile).ReloadAsync(cancellationToken);
        if (!profile.IsApproved)
        {
            return new(RiderOperationCode.ProfileInactive);
        }

        var hasActiveDelivery = await support.HasActiveDeliveryAsync(profile.StakeholderId, cancellationToken);
        if (isAvailable && hasActiveDelivery)
        {
            return new(RiderOperationCode.Conflict);
        }

        if (isAvailable && !await support.IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, cancellationToken))
        {
            return new(RiderOperationCode.Conflict);
        }

        var location = await dbContext.RiderCurrentLocations
            .SingleOrDefaultAsync(item => item.RiderProfileId == profile.Id, cancellationToken);
        if (isAvailable && !RiderPersistenceSupport.IsFresh(location, utcNow, options))
        {
            return new(RiderOperationCode.StaleLocation);
        }

        profile.SetAvailability(isAvailable);
        if (!isAvailable)
        {
            var offers = await dbContext.RiderDeliveryOffers
                .Where(offer => offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
                .ToListAsync(cancellationToken);
            var sessionIds = offers.Select(offer => offer.MatchingSessionId).Distinct().ToArray();
            var sessions = await dbContext.DeliveryMatchingSessions
                .Where(session => sessionIds.Contains(session.Id))
                .ToDictionaryAsync(session => session.Id, cancellationToken);
            foreach (var offer in offers)
            {
                offer.Revoke(utcNow);
                if (sessions.TryGetValue(offer.MatchingSessionId, out var session) &&
                    session.Status == DeliveryMatchingSessionStatus.Active)
                {
                    session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await support.BuildStatusResultAsync(profile, location, utcNow, options, cancellationToken);
    }
}
