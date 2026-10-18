using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderMatchingPersistence
{
    Task<IReadOnlyList<ExpiredRiderOfferCandidate>> ListExpiredOfferCandidatesAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);

    Task<RiderOfferExpiryState> LoadOfferExpiryStateAsync(
        Guid offerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListScheduledDeliveryIdsAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);

    Task<Delivery?> LoadScheduledDeliveryAsync(
        Guid deliveryId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListMatchingEligibleDeliveryIdsAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);

    Task<RiderMatchingState> LoadMatchingStateAsync(
        Guid deliveryId,
        CancellationToken cancellationToken);

    Task<RiderCandidate?> FindCandidateAsync(
        Delivery delivery,
        int searchRadiusMeters,
        DateTimeOffset freshAfterUtc,
        DateTimeOffset utcNow,
        int maximumCandidates,
        CancellationToken cancellationToken);

    Task<bool> IsCandidateEligibleAsync(
        Delivery delivery,
        Guid riderProfileId,
        int searchRadiusMeters,
        DateTimeOffset freshAfterUtc,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    void AddMatchingSession(DeliveryMatchingSession session);
    void AddOffer(RiderDeliveryOffer offer);
    void AddStatusHistory(DeliveryStatusHistory history);
}

public sealed record ExpiredRiderOfferCandidate(
    Guid OfferId,
    Guid DeliveryId,
    Guid RiderProfileId,
    Guid RiderStakeholderId);

public sealed record RiderOfferExpiryState(
    RiderDeliveryOffer? Offer,
    DeliveryMatchingSession? MatchingSession);

public sealed record RiderMatchingState(
    Delivery? Delivery,
    DeliveryMatchingSession? ActiveSession,
    bool HasPendingOffer);

public sealed record RiderCandidate(
    Guid RiderProfileId,
    Guid RiderStakeholderId,
    double DistanceMeters);
