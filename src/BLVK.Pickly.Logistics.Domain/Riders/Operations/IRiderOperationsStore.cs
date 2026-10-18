using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public interface IRiderOperationsStore
{
    Task<RiderOperationResult> UpdateLocationAsync(
        ActorContext actor,
        decimal latitude,
        decimal longitude,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);

    Task<RiderStatusResult> SetAvailabilityAsync(
        ActorContext actor,
        bool isAvailable,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);

    Task<RiderStatusResult> GetStatusAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);

    Task<RiderOffersResult> ListOffersAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<RiderOfferDecisionResult> AcceptOfferAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);

    Task<RiderOperationResult> RejectOfferAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);

    Task<RiderActiveDeliveryResult> GetActiveDeliveryAsync(
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<SenderAssignmentSnapshot> GetSenderAssignmentAsync(
        Guid deliveryId,
        CancellationToken cancellationToken);

    Task<MatchingProcessingResult> ProcessMatchingAsync(
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);
}
