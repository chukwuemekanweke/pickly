using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderOfferRejectionPersistence(AppDbContext dbContext) : IRiderOfferRejectionPersistence
{
    public async Task<RiderOfferPreview?> FindPreviewAsync(Guid offerId, CancellationToken cancellationToken) =>
        await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .Where(offer => offer.Id == offerId)
            .Select(offer => new RiderOfferPreview(offer.DeliveryId, offer.RiderProfileId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<RiderOfferRejectionState> LoadAsync(Guid offerId, CancellationToken cancellationToken)
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
}
