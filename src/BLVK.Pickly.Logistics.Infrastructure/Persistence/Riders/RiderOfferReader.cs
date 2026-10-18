using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderOfferReader(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderOfferReader
{
    public async Task<RiderOffersResult> ListAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        if (!resolved.Profile!.IsApproved)
        {
            return new(RiderOperationCode.ProfileInactive);
        }

        // Listing lazily expires the rider's own timed-out offers so they are never presented as actionable.
        var expired = await dbContext.RiderDeliveryOffers
            .Where(offer =>
                offer.RiderProfileId == resolved.Profile.Id &&
                offer.Status == RiderDeliveryOfferStatus.Pending &&
                offer.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
        foreach (var offer in expired)
        {
            offer.Expire(utcNow);
        }

        if (expired.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var offers = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .Where(offer =>
                offer.RiderProfileId == resolved.Profile.Id &&
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

        var snapshots = offers
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

        return new(RiderOperationCode.Success, snapshots);
    }
}
