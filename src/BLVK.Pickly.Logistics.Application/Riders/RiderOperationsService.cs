using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders;

public sealed class RiderOperationsService(
    IRiderOperationsStore store,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public Task<RiderOperationResult> UpdateLocationAsync(
        ActorContext actor,
        decimal latitude,
        decimal longitude,
        DateTimeOffset capturedAtUtc,
        CancellationToken cancellationToken) =>
        store.UpdateLocationAsync(
            actor,
            latitude,
            longitude,
            capturedAtUtc,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);

    public Task<RiderStatusResult> SetAvailabilityAsync(
        ActorContext actor,
        bool isAvailable,
        CancellationToken cancellationToken) =>
        store.SetAvailabilityAsync(actor, isAvailable, timeProvider.GetUtcNow(), options.Value, cancellationToken);

    public Task<RiderStatusResult> GetStatusAsync(ActorContext actor, CancellationToken cancellationToken) =>
        store.GetStatusAsync(actor, timeProvider.GetUtcNow(), options.Value, cancellationToken);

    public Task<RiderOffersResult> ListOffersAsync(ActorContext actor, CancellationToken cancellationToken) =>
        store.ListOffersAsync(actor, timeProvider.GetUtcNow(), cancellationToken);

    public Task<RiderOfferDecisionResult> AcceptOfferAsync(
        ActorContext actor,
        Guid offerId,
        CancellationToken cancellationToken) =>
        store.AcceptOfferAsync(actor, offerId, timeProvider.GetUtcNow(), options.Value, cancellationToken);

    public Task<RiderOperationResult> RejectOfferAsync(
        ActorContext actor,
        Guid offerId,
        CancellationToken cancellationToken) =>
        store.RejectOfferAsync(actor, offerId, timeProvider.GetUtcNow(), options.Value, cancellationToken);

    public Task<RiderActiveDeliveryResult> GetActiveDeliveryAsync(
        ActorContext actor,
        CancellationToken cancellationToken) =>
        store.GetActiveDeliveryAsync(actor, cancellationToken);

    public Task<MatchingProcessingResult> ProcessMatchingAsync(CancellationToken cancellationToken) =>
        store.ProcessMatchingAsync(timeProvider.GetUtcNow(), options.Value, cancellationToken);
}
