using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class SenderDeliveryAssignmentReader(AppDbContext dbContext) : ISenderDeliveryAssignmentReader
{
    public async Task<SenderDeliveryAssignmentFacts> GetAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await dbContext.Deliveries
            .AsNoTracking()
            .SingleAsync(item => item.Id == deliveryId, cancellationToken);
        var activeOffer = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .AnyAsync(item => item.DeliveryId == deliveryId && item.Status == RiderDeliveryOfferStatus.Pending, cancellationToken);
        var activeSession = await dbContext.DeliveryMatchingSessions
            .AsNoTracking()
            .AnyAsync(item => item.DeliveryId == deliveryId && item.Status == DeliveryMatchingSessionStatus.Active, cancellationToken);

        string? riderName = null;
        string? riderAvatarUrl = null;
        DateTimeOffset? assignedAtUtc = null;
        if (delivery.RiderId.HasValue)
        {
            var rider = await dbContext.Stakeholders
                .AsNoTracking()
                .Where(item => item.Id == delivery.RiderId.Value)
                .Select(item => new { item.FirstName, item.LastName, item.AvatarUrl })
                .SingleOrDefaultAsync(cancellationToken);
            if (rider is not null)
            {
                riderName = $"{rider.FirstName} {rider.LastName}".Trim();
                riderAvatarUrl = rider.AvatarUrl;
            }

            assignedAtUtc = await dbContext.DeliveryStatusHistory
                .AsNoTracking()
                .Where(item => item.DeliveryId == deliveryId && item.NewStatus == DeliveryStatus.RiderAssigned)
                .OrderByDescending(item => item.OccurredAtUtc)
                .Select(item => (DateTimeOffset?)item.OccurredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new(
            activeOffer,
            activeSession,
            delivery.IsMatchingEligible,
            delivery.RiderId,
            riderName,
            riderAvatarUrl,
            assignedAtUtc);
    }
}
