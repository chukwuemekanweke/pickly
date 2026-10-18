using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferRejectionPersistence
{
    Task<RiderOfferPreview?> FindPreviewAsync(Guid offerId, CancellationToken cancellationToken);

    Task<RiderOfferRejectionState> LoadAsync(Guid offerId, CancellationToken cancellationToken);
}

public sealed record RiderOfferRejectionState(
    RiderDeliveryOffer? Offer,
    DeliveryMatchingSession? MatchingSession);
