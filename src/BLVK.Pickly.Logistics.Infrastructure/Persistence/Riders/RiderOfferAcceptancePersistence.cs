using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderOfferAcceptancePersistence(
    AppDbContext dbContext,
    RiderGeographyQueries geographyQueries) : IRiderOfferAcceptancePersistence
{
    private static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<RiderOfferPreview?> FindPreviewAsync(Guid offerId, CancellationToken cancellationToken) =>
        await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .Where(offer => offer.Id == offerId)
            .Select(offer => new RiderOfferPreview(offer.DeliveryId, offer.RiderProfileId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RiderOfferAcceptanceState> LoadAsync(
        Guid offerId,
        Guid riderProfileId,
        CancellationToken cancellationToken)
    {
        var offer = await dbContext.RiderDeliveryOffers.SingleOrDefaultAsync(
            item => item.Id == offerId,
            cancellationToken);
        if (offer is null)
        {
            return new(null, null, null, null);
        }

        var delivery = await dbContext.Deliveries
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == offer.DeliveryId, cancellationToken);
        var session = await dbContext.DeliveryMatchingSessions.SingleOrDefaultAsync(
            item => item.Id == offer.MatchingSessionId,
            cancellationToken);
        var location = await dbContext.RiderCurrentLocations.SingleOrDefaultAsync(
            item => item.RiderProfileId == riderProfileId,
            cancellationToken);
        return new(offer, delivery, session, location);
    }

    public Task<bool> IsVehicleCategoryActiveAsync(
        Guid vehicleCategoryId,
        CancellationToken cancellationToken) =>
        dbContext.VehicleCategories.AnyAsync(
            vehicle => vehicle.Id == vehicleCategoryId && vehicle.IsActive,
            cancellationToken);

    public async Task<RiderGeographicEligibility> GetGeographicEligibilityAsync(
        RiderProfile profile,
        Delivery delivery,
        RiderCurrentLocation? location,
        int searchRadiusMeters,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (location is null)
        {
            return new(false, false, false);
        }

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
            return new(false, false, false);
        }

        var servesZone = await dbContext.RiderServiceZones.AnyAsync(
            item => item.RiderProfileId == profile.Id && item.DeliveryZoneId == zoneId.Value,
            cancellationToken);
        var isWithinRadius = await dbContext.RiderCurrentLocations.AnyAsync(
            item => item.Id == location.Id &&
                EF.Property<Point>(item, RiderCurrentLocationConfiguration.PositionProperty)
                    .Distance(pickupPoint) <= searchRadiusMeters,
            cancellationToken);
        return new(true, servesZone, isWithinRadius);
    }

    public Task<bool> HasActiveDeliveryAsync(Guid riderStakeholderId, CancellationToken cancellationToken) =>
        dbContext.Deliveries.AnyAsync(
            delivery => delivery.RiderId == riderStakeholderId && ActiveDeliveryStatuses.Contains(delivery.Status),
            cancellationToken);

    public async Task<IReadOnlyList<RiderDeliveryOffer>> ListConflictingOffersAsync(
        Guid acceptedOfferId,
        Guid deliveryId,
        Guid riderProfileId,
        CancellationToken cancellationToken) =>
        await dbContext.RiderDeliveryOffers
            .Where(offer =>
                offer.Id != acceptedOfferId &&
                offer.Status == RiderDeliveryOfferStatus.Pending &&
                (offer.DeliveryId == deliveryId || offer.RiderProfileId == riderProfileId))
            .ToListAsync(cancellationToken);

    public void AddStatusHistory(DeliveryStatusHistory history) =>
        dbContext.DeliveryStatusHistory.Add(history);
}
