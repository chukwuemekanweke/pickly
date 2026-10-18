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

    public async Task<IReadOnlyList<ExpiredRiderOfferCandidate>> ListExpiredOfferCandidatesAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken) =>
        await (from offer in dbContext.RiderDeliveryOffers.AsNoTracking()
            join profile in dbContext.RiderProfiles.AsNoTracking() on offer.RiderProfileId equals profile.Id
            where offer.Status == RiderDeliveryOfferStatus.Pending && offer.ExpiresAtUtc <= utcNow
            orderby offer.ExpiresAtUtc, offer.Id
            select new ExpiredRiderOfferCandidate(
                offer.Id,
                offer.DeliveryId,
                offer.RiderProfileId,
                profile.StakeholderId))
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<RiderOfferExpiryState> LoadOfferExpiryStateAsync(
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var offer = await dbContext.RiderDeliveryOffers.SingleOrDefaultAsync(
            item => item.Id == offerId,
            cancellationToken);
        var session = offer is null
            ? null
            : await dbContext.DeliveryMatchingSessions.SingleOrDefaultAsync(
                item => item.Id == offer.MatchingSessionId,
                cancellationToken);
        return new(offer, session);
    }

    public async Task<IReadOnlyList<Guid>> ListScheduledDeliveryIdsAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.Deliveries
            .AsNoTracking()
            .Where(delivery =>
                delivery.Status == DeliveryStatus.AwaitingDispatch &&
                delivery.DispatchEligibleAtUtc <= utcNow &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery ||
                    delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.DispatchEligibleAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Select(delivery => delivery.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task<Delivery?> LoadScheduledDeliveryAsync(
        Guid deliveryId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        dbContext.Deliveries.SingleOrDefaultAsync(
            delivery =>
                delivery.Id == deliveryId &&
                delivery.Status == DeliveryStatus.AwaitingDispatch &&
                delivery.DispatchEligibleAtUtc <= utcNow &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery ||
                    delivery.PaymentStatus == DeliveryPaymentStatus.Paid),
            cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListMatchingEligibleDeliveryIdsAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.Deliveries
            .AsNoTracking()
            .Where(delivery =>
                delivery.Status == DeliveryStatus.PendingAssignment &&
                delivery.RiderId == null &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery ||
                    delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .Where(delivery => !dbContext.DeliveryMatchingSessions.Any(session =>
                session.DeliveryId == delivery.Id &&
                session.Status == DeliveryMatchingSessionStatus.Active &&
                session.NextAttemptAtUtc > utcNow))
            .Where(delivery => !dbContext.RiderDeliveryOffers.Any(offer =>
                offer.DeliveryId == delivery.Id && offer.Status == RiderDeliveryOfferStatus.Pending))
            .OrderBy(delivery => delivery.CreatedAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Select(delivery => delivery.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<RiderMatchingState> LoadMatchingStateAsync(
        Guid deliveryId,
        CancellationToken cancellationToken)
    {
        var delivery = await dbContext.Deliveries.SingleOrDefaultAsync(
            item => item.Id == deliveryId,
            cancellationToken);
        var session = await dbContext.DeliveryMatchingSessions.SingleOrDefaultAsync(
            item => item.DeliveryId == deliveryId && item.Status == DeliveryMatchingSessionStatus.Active,
            cancellationToken);
        var hasPendingOffer = await dbContext.RiderDeliveryOffers.AnyAsync(
            offer => offer.DeliveryId == deliveryId && offer.Status == RiderDeliveryOfferStatus.Pending,
            cancellationToken);
        return new(delivery, session, hasPendingOffer);
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

        return await EligibleCandidates(
                delivery,
                zoneId.Value,
                pickupPoint,
                searchRadiusMeters,
                freshAfterUtc,
                riderProfileId: null)
            .Take(maximumCandidates)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsCandidateEligibleAsync(
        Delivery delivery,
        Guid riderProfileId,
        int searchRadiusMeters,
        DateTimeOffset freshAfterUtc,
        DateTimeOffset utcNow,
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
        return zoneId.HasValue && await EligibleCandidates(
                delivery,
                zoneId.Value,
                pickupPoint,
                searchRadiusMeters,
                freshAfterUtc,
                riderProfileId)
            .AnyAsync(cancellationToken);
    }

    private IQueryable<RiderCandidate> EligibleCandidates(
        Delivery delivery,
        Guid zoneId,
        Point pickupPoint,
        int searchRadiusMeters,
        DateTimeOffset freshAfterUtc,
        Guid? riderProfileId) =>
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
            where !riderProfileId.HasValue || profile.Id == riderProfileId.Value
            where profile.AvailabilityStatus == RiderAvailabilityStatus.Available
            where profile.VehicleCategoryId == delivery.VehicleCategoryId
            where vehicle.IsActive
            where stakeholder.IsVerified && stakeholderType.Key == RiderStakeholderTypeKey
            where location.CapturedAtUtc >= freshAfterUtc
            where serviceZone.DeliveryZoneId == zoneId
            where !dbContext.Deliveries.Any(item =>
                item.RiderId == profile.StakeholderId && ActiveDeliveryStatuses.Contains(item.Status))
            where !dbContext.RiderDeliveryOffers.Any(offer =>
                offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
            let distance = EF.Property<Point>(location, RiderCurrentLocationConfiguration.PositionProperty)
                .Distance(pickupPoint)
            where distance <= searchRadiusMeters
            orderby distance, profile.Id
            select new RiderCandidate(profile.Id, profile.StakeholderId, distance);

    public void AddMatchingSession(DeliveryMatchingSession session) =>
        dbContext.DeliveryMatchingSessions.Add(session);

    public void AddOffer(RiderDeliveryOffer offer) =>
        dbContext.RiderDeliveryOffers.Add(offer);

    public void AddStatusHistory(DeliveryStatusHistory history) =>
        dbContext.DeliveryStatusHistory.Add(history);
}
