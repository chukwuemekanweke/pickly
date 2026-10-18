using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderStatusReader(AppDbContext dbContext) : IRiderStatusReader
{
    private static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<RiderStatusFacts> GetFactsAsync(
        RiderProfile profile,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var location = await dbContext.RiderCurrentLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.RiderProfileId == profile.Id, cancellationToken);
        var hasActiveDelivery = await dbContext.Deliveries.AnyAsync(
            delivery => delivery.RiderId == profile.StakeholderId && ActiveDeliveryStatuses.Contains(delivery.Status),
            cancellationToken);
        var pendingOfferCount = await dbContext.RiderDeliveryOffers.CountAsync(
            offer => offer.RiderProfileId == profile.Id &&
                offer.Status == RiderDeliveryOfferStatus.Pending &&
                offer.ExpiresAtUtc > utcNow,
            cancellationToken);
        return new(location, hasActiveDelivery, pendingOfferCount);
    }
}
