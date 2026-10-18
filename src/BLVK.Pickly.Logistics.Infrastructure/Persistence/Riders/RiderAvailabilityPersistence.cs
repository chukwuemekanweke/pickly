using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderAvailabilityPersistence(AppDbContext dbContext) : IRiderAvailabilityPersistence
{
    private static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<RiderAvailabilityState> LoadAsync(
        RiderProfile profile,
        CancellationToken cancellationToken)
    {
        var location = await dbContext.RiderCurrentLocations.SingleOrDefaultAsync(
            item => item.RiderProfileId == profile.Id,
            cancellationToken);
        var hasActiveDelivery = await dbContext.Deliveries.AnyAsync(
            delivery => delivery.RiderId == profile.StakeholderId && ActiveDeliveryStatuses.Contains(delivery.Status),
            cancellationToken);
        var isVehicleActive = await dbContext.VehicleCategories.AnyAsync(
            vehicle => vehicle.Id == profile.VehicleCategoryId && vehicle.IsActive,
            cancellationToken);
        var pendingOffers = await dbContext.RiderDeliveryOffers
            .Where(offer => offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
            .ToListAsync(cancellationToken);
        var sessionIds = pendingOffers.Select(offer => offer.MatchingSessionId).Distinct().ToArray();
        var sessions = await dbContext.DeliveryMatchingSessions
            .Where(session => sessionIds.Contains(session.Id))
            .ToDictionaryAsync(session => session.Id, cancellationToken);
        return new(location, hasActiveDelivery, isVehicleActive, pendingOffers, sessions);
    }
}
