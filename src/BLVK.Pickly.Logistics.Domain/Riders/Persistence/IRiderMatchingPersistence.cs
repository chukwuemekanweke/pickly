using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderMatchingPersistence
{
    Task<IReadOnlyList<RiderDeliveryOffer>> ListExpiredOffersAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, DeliveryMatchingSession>> GetMatchingSessionsAsync(
        IEnumerable<Guid> matchingSessionIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Delivery>> ListScheduledDeliveriesAsync(
        DateTimeOffset utcNow,
        int batchSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Delivery>> ListMatchingEligibleDeliveriesAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, DeliveryMatchingSession>> GetActiveSessionsByDeliveryAsync(
        IEnumerable<Guid> deliveryIds,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetDeliveriesWithPendingOffersAsync(
        IEnumerable<Guid> deliveryIds,
        CancellationToken cancellationToken);

    Task<RiderCandidate?> FindCandidateAsync(
        Delivery delivery,
        int searchRadiusMeters,
        DateTimeOffset freshAfterUtc,
        DateTimeOffset utcNow,
        int maximumCandidates,
        CancellationToken cancellationToken);

    void AddMatchingSession(DeliveryMatchingSession session);
    void AddOffer(RiderDeliveryOffer offer);
    void AddStatusHistory(DeliveryStatusHistory history);
}

public sealed record RiderCandidate(Guid RiderProfileId, double DistanceMeters);
