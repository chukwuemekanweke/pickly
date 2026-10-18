using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderOfferReader(AppDbContext dbContext) : IRiderOfferReader
{
    public async Task<IReadOnlyCollection<RiderOfferSnapshot>> ListActiveSnapshotsAsync(
        Guid riderProfileId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var offers = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .Where(offer =>
                offer.RiderProfileId == riderProfileId &&
                offer.Status == RiderDeliveryOfferStatus.Pending &&
                offer.ExpiresAtUtc > utcNow)
            .OrderBy(offer => offer.ExpiresAtUtc)
            .ThenBy(offer => offer.Id)
            .ToListAsync(cancellationToken);
        var deliveryIds = offers.Select(offer => offer.DeliveryId).ToArray();
        var deliveries = await dbContext.Deliveries
            .AsNoTracking()
            .Include(delivery => delivery.Items)
            .Where(delivery => deliveryIds.Contains(delivery.Id))
            .ToDictionaryAsync(delivery => delivery.Id, cancellationToken);

        return offers
            .Where(offer => deliveries.ContainsKey(offer.DeliveryId))
            .Select(offer =>
            {
                var delivery = deliveries[offer.DeliveryId];
                return new RiderOfferSnapshot(
                    offer.Id,
                    delivery.Id,
                    delivery.TrackingId,
                    delivery.PickupAddress.AddressLine,
                    $"{delivery.DropOffAddress.City}, {delivery.DropOffAddress.State}",
                    delivery.VehicleCategoryId,
                    delivery.Items.Sum(item => item.Quantity),
                    delivery.Items.Sum(item => item.Quantity * item.WeightKg),
                    offer.ExpiresAtUtc);
            })
            .ToArray();
    }
}
