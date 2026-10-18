using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.Infrastructure.Riders;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderMatchingPersistence(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderMatchingPersistence
{
    private const string MatchingProcessorLockKey = "rider-matching-processor";

    public async Task<MatchingProcessingResult> ProcessAsync(
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await support.AcquireKeyLockAsync(MatchingProcessorLockKey, cancellationToken);

        var expiredOffers = await dbContext.RiderDeliveryOffers
            .Where(offer => offer.Status == RiderDeliveryOfferStatus.Pending && offer.ExpiresAtUtc <= utcNow)
            .OrderBy(offer => offer.ExpiresAtUtc)
            .Take(options.MatchingScanBatchSize)
            .ToListAsync(cancellationToken);
        var expiredSessionIds = expiredOffers.Select(offer => offer.MatchingSessionId).Distinct().ToArray();
        var expiredSessions = await dbContext.DeliveryMatchingSessions
            .Where(session => expiredSessionIds.Contains(session.Id))
            .ToDictionaryAsync(session => session.Id, cancellationToken);
        foreach (var offer in expiredOffers)
        {
            offer.Expire(utcNow);
            if (expiredSessions.TryGetValue(offer.MatchingSessionId, out var session) &&
                session.Status == DeliveryMatchingSessionStatus.Active)
            {
                session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
            }
        }

        var scheduledDeliveries = await dbContext.Deliveries
            .Where(delivery =>
                delivery.Status == DeliveryStatus.AwaitingDispatch &&
                delivery.DispatchEligibleAtUtc <= utcNow &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery || delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.DispatchEligibleAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Take(options.ScheduledDispatchScanBatchSize)
            .ToListAsync(cancellationToken);
        foreach (var delivery in scheduledDeliveries)
        {
            delivery.OpenDispatch(utcNow, "system", "scheduled_dispatch_job");
            dbContext.DeliveryStatusHistory.Add(delivery.StatusHistory.Last());
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var deliveries = await dbContext.Deliveries
            .Where(delivery =>
                delivery.Status == DeliveryStatus.PendingAssignment &&
                delivery.RiderId == null &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery || delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.CreatedAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Take(options.MatchingScanBatchSize)
            .ToListAsync(cancellationToken);

        // Filtered unique indexes guarantee at most one active session and one pending offer per Delivery.
        var deliveryIds = deliveries.Select(delivery => delivery.Id).ToArray();
        var activeSessions = await dbContext.DeliveryMatchingSessions
            .Where(session => deliveryIds.Contains(session.DeliveryId) && session.Status == DeliveryMatchingSessionStatus.Active)
            .ToDictionaryAsync(session => session.DeliveryId, cancellationToken);
        var deliveriesWithPendingOffer = (await dbContext.RiderDeliveryOffers
            .Where(offer => deliveryIds.Contains(offer.DeliveryId) && offer.Status == RiderDeliveryOfferStatus.Pending)
            .Select(offer => offer.DeliveryId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var attempts = 0;
        var created = 0;
        var noCandidates = 0;
        foreach (var delivery in deliveries)
        {
            if (!activeSessions.TryGetValue(delivery.Id, out var session))
            {
                session = DeliveryMatchingSession.Create(delivery.Id, options.InitialSearchRadiusMeters, utcNow);
                dbContext.DeliveryMatchingSessions.Add(session);
            }

            if (session.NextAttemptAtUtc > utcNow || deliveriesWithPendingOffer.Contains(delivery.Id))
            {
                continue;
            }

            attempts++;
            var pickupPoint = RiderPersistenceSupport.CreatePoint(delivery.PickupAddress.Latitude, delivery.PickupAddress.Longitude);
            var zoneId = await support.FindPickupZoneIdAsync(delivery, pickupPoint, utcNow, cancellationToken);
            RiderCandidate? candidate = null;
            if (zoneId.HasValue)
            {
                var freshAfter = utcNow.ToUniversalTime() - options.LocationFreshness;
                candidate = await (
                    from profile in dbContext.RiderProfiles
                    join location in dbContext.RiderCurrentLocations on profile.Id equals location.RiderProfileId
                    join serviceZone in dbContext.RiderServiceZones on profile.Id equals serviceZone.RiderProfileId
                    join stakeholder in dbContext.Stakeholders on profile.StakeholderId equals stakeholder.Id
                    join stakeholderType in dbContext.StakeholderTypes on stakeholder.StakeholderTypeId equals stakeholderType.Id
                    join vehicle in dbContext.VehicleCategories on profile.VehicleCategoryId equals vehicle.Id
                    where profile.TenantId == dbContext.Stakeholders
                        .Where(stakeholder => stakeholder.Id == delivery.SenderId)
                        .Select(stakeholder => stakeholder.TenantId)
                        .Single()
                    where profile.VerificationStatus == RiderVerificationStatus.Approved
                    where profile.AvailabilityStatus == RiderAvailabilityStatus.Available
                    where profile.VehicleCategoryId == delivery.VehicleCategoryId
                    where vehicle.IsActive
                    where stakeholder.IsVerified && stakeholderType.Key == RiderPersistenceSupport.RiderStakeholderTypeKey
                    where location.CapturedAtUtc >= freshAfter
                    where serviceZone.DeliveryZoneId == zoneId.Value
                    where !dbContext.Deliveries.Any(item =>
                        item.RiderId == profile.StakeholderId &&
                        RiderPersistenceSupport.ActiveDeliveryStatuses.Contains(item.Status))
                    where !dbContext.RiderDeliveryOffers.Any(offer =>
                        offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
                    let distance = EF.Property<Point>(location, RiderCurrentLocationConfiguration.PositionProperty)
                        .Distance(pickupPoint)
                    where distance <= session.SearchRadiusMeters
                    orderby distance, profile.Id
                    select new RiderCandidate(profile.Id, distance))
                    .Take(options.MaximumCandidatesPerRound)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var nextRadius = Math.Min(
                options.MaximumSearchRadiusMeters,
                session.SearchRadiusMeters + options.SearchRadiusExpansionMeters);
            var cycleBoundary = (session.AttemptCount + 1) % options.MaximumAttemptsPerCycle == 0;
            session.RecordAttempt(
                utcNow,
                candidate is null || cycleBoundary ? utcNow + options.RetryBackoff : utcNow,
                cycleBoundary ? options.InitialSearchRadiusMeters : nextRadius);

            if (candidate is null)
            {
                noCandidates++;
                continue;
            }

            dbContext.RiderDeliveryOffers.Add(RiderDeliveryOffer.Create(
                session.Id,
                delivery.Id,
                candidate.RiderProfileId,
                utcNow,
                utcNow + options.OfferTimeout));
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var result = new MatchingProcessingResult(
            scheduledDeliveries.Count,
            expiredOffers.Count,
            attempts,
            created,
            noCandidates);
        RiderMatchingMetrics.RecordProcessing(result);
        return result;
    }

    private sealed record RiderCandidate(Guid RiderProfileId, double DistanceMeters);
}
