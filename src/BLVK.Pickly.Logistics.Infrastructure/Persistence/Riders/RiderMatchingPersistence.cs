using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderMatchingPersistence(
    AppDbContext dbContext,
    RiderGeographyQueries geographyQueries) : IRiderMatchingPersistence
{
    private const string RiderStakeholderTypeKey = "rider";
    private static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<IReadOnlyList<RiderDeliveryOffer>> ListExpiredOffersAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.RiderDeliveryOffers
            .Where(offer => offer.Status == RiderDeliveryOfferStatus.Pending && offer.ExpiresAtUtc <= utcNow)
            .OrderBy(offer => offer.ExpiresAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DeliveryMatchingSession>> GetMatchingSessionsAsync(
        IEnumerable<Guid> matchingSessionIds,
        CancellationToken cancellationToken)
    {
        var ids = matchingSessionIds.Distinct().ToArray();
        return await dbContext.DeliveryMatchingSessions
            .Where(session => ids.Contains(session.Id))
            .ToDictionaryAsync(session => session.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<Delivery>> ListScheduledDeliveriesAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.Deliveries
            .Where(delivery =>
                delivery.Status == DeliveryStatus.AwaitingDispatch &&
                delivery.DispatchEligibleAtUtc <= utcNow &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery ||
                    delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.DispatchEligibleAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Delivery>> ListMatchingEligibleDeliveriesAsync(
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.Deliveries
            .Where(delivery =>
                delivery.Status == DeliveryStatus.PendingAssignment &&
                delivery.RiderId == null &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery ||
                    delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.CreatedAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DeliveryMatchingSession>> GetActiveSessionsByDeliveryAsync(
        IEnumerable<Guid> deliveryIds,
        CancellationToken cancellationToken)
    {
        var ids = deliveryIds.Distinct().ToArray();
        return await dbContext.DeliveryMatchingSessions
            .Where(session => ids.Contains(session.DeliveryId) && session.Status == DeliveryMatchingSessionStatus.Active)
            .ToDictionaryAsync(session => session.DeliveryId, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetDeliveriesWithPendingOffersAsync(
        IEnumerable<Guid> deliveryIds,
        CancellationToken cancellationToken)
    {
        var ids = deliveryIds.Distinct().ToArray();
        return (await dbContext.RiderDeliveryOffers
                .Where(offer => ids.Contains(offer.DeliveryId) && offer.Status == RiderDeliveryOfferStatus.Pending)
                .Select(offer => offer.DeliveryId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    public async Task<RiderCandidate?> FindCandidateAsync(
        Delivery delivery,
        int searchRadiusMeters,
        DateTimeOffset freshAfterUtc,
        DateTimeOffset utcNow,
        int maximumCandidates,
        CancellationToken cancellationToken)
    {
        var pickupPoint = RiderGeographyQueries.CreatePoint(
            delivery.PickupAddress.Latitude,
            delivery.PickupAddress.Longitude);
        var zoneId = await geographyQueries.FindPickupZoneIdAsync(
            delivery,
            pickupPoint,
            utcNow,
            cancellationToken);
        if (!zoneId.HasValue)
        {
            return null;
        }

        return await (
            from profile in dbContext.RiderProfiles
            join location in dbContext.RiderCurrentLocations on profile.Id equals location.RiderProfileId
            join serviceZone in dbContext.RiderServiceZones on profile.Id equals serviceZone.RiderProfileId
            join stakeholder in dbContext.Stakeholders on profile.StakeholderId equals stakeholder.Id
            join stakeholderType in dbContext.StakeholderTypes on stakeholder.StakeholderTypeId equals stakeholderType.Id
            join vehicle in dbContext.VehicleCategories on profile.VehicleCategoryId equals vehicle.Id
            where profile.TenantId == dbContext.Stakeholders
                .Where(sender => sender.Id == delivery.SenderId)
                .Select(sender => sender.TenantId)
                .Single()
            where profile.VerificationStatus == RiderVerificationStatus.Approved
            where profile.AvailabilityStatus == RiderAvailabilityStatus.Available
            where profile.VehicleCategoryId == delivery.VehicleCategoryId
            where vehicle.IsActive
            where stakeholder.IsVerified && stakeholderType.Key == RiderStakeholderTypeKey
            where location.CapturedAtUtc >= freshAfterUtc
            where serviceZone.DeliveryZoneId == zoneId.Value
            where !dbContext.Deliveries.Any(item =>
                item.RiderId == profile.StakeholderId && ActiveDeliveryStatuses.Contains(item.Status))
            where !dbContext.RiderDeliveryOffers.Any(offer =>
                offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
            let distance = EF.Property<Point>(location, RiderCurrentLocationConfiguration.PositionProperty)
                .Distance(pickupPoint)
            where distance <= searchRadiusMeters
            orderby distance, profile.Id
            select new RiderCandidate(profile.Id, distance))
            .Take(maximumCandidates)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void AddMatchingSession(DeliveryMatchingSession session) =>
        dbContext.DeliveryMatchingSessions.Add(session);

    public void AddOffer(RiderDeliveryOffer offer) =>
        dbContext.RiderDeliveryOffers.Add(offer);

    public void AddStatusHistory(DeliveryStatusHistory history) =>
        dbContext.DeliveryStatusHistory.Add(history);
}
