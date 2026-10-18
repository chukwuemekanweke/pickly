using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;

public sealed class ProcessRiderMatchingHandler(
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderMatchingPersistence persistence,
    IRiderMatchingTelemetry telemetry,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<ProcessRiderMatchingResult> HandleAsync(
        ProcessRiderMatchingCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var settings = options.Value;
        await using var transaction = await transactionFactory.BeginAsync(
            RiderTransactionIsolation.Serializable,
            cancellationToken);
        await consistencyLock.AcquireMatchingProcessorAsync(cancellationToken);

        var expiredOffers = await persistence.ListExpiredOffersAsync(
            now,
            settings.MatchingScanBatchSize,
            cancellationToken);
        var expiredSessions = await persistence.GetMatchingSessionsAsync(
            expiredOffers.Select(offer => offer.MatchingSessionId),
            cancellationToken);
        foreach (var offer in expiredOffers)
        {
            offer.Expire(now);
            if (expiredSessions.TryGetValue(offer.MatchingSessionId, out var session) &&
                session.Status == DeliveryMatchingSessionStatus.Active)
            {
                session.RecordAttempt(now, now + settings.RetryBackoff, session.SearchRadiusMeters);
            }
        }

        var scheduledDeliveries = await persistence.ListScheduledDeliveriesAsync(
            now,
            settings.ScheduledDispatchScanBatchSize,
            cancellationToken);
        foreach (var delivery in scheduledDeliveries)
        {
            delivery.OpenDispatch(now, "system", "scheduled_dispatch_job");
            persistence.AddStatusHistory(delivery.StatusHistory.Last());
        }

        await transaction.FlushAsync(cancellationToken);

        var deliveries = await persistence.ListMatchingEligibleDeliveriesAsync(
            settings.MatchingScanBatchSize,
            cancellationToken);
        var deliveryIds = deliveries.Select(delivery => delivery.Id).ToArray();
        var activeSessions = await persistence.GetActiveSessionsByDeliveryAsync(deliveryIds, cancellationToken);
        var deliveriesWithPendingOffer = await persistence.GetDeliveriesWithPendingOffersAsync(
            deliveryIds,
            cancellationToken);

        var attempts = 0;
        var created = 0;
        var noCandidates = 0;
        foreach (var delivery in deliveries)
        {
            if (!activeSessions.TryGetValue(delivery.Id, out var session))
            {
                session = DeliveryMatchingSession.Create(delivery.Id, settings.InitialSearchRadiusMeters, now);
                persistence.AddMatchingSession(session);
            }

            if (session.NextAttemptAtUtc > now || deliveriesWithPendingOffer.Contains(delivery.Id))
            {
                continue;
            }

            attempts++;
            var candidate = await persistence.FindCandidateAsync(
                delivery,
                session.SearchRadiusMeters,
                now.ToUniversalTime() - settings.LocationFreshness,
                now,
                settings.MaximumCandidatesPerRound,
                cancellationToken);
            var nextRadius = Math.Min(
                settings.MaximumSearchRadiusMeters,
                session.SearchRadiusMeters + settings.SearchRadiusExpansionMeters);
            var cycleBoundary = (session.AttemptCount + 1) % settings.MaximumAttemptsPerCycle == 0;
            session.RecordAttempt(
                now,
                candidate is null || cycleBoundary ? now + settings.RetryBackoff : now,
                cycleBoundary ? settings.InitialSearchRadiusMeters : nextRadius);

            if (candidate is null)
            {
                noCandidates++;
                continue;
            }

            persistence.AddOffer(RiderDeliveryOffer.Create(
                session.Id,
                delivery.Id,
                candidate.RiderProfileId,
                now,
                now + settings.OfferTimeout));
            created++;
        }

        await transaction.CommitAsync(cancellationToken);
        var result = new ProcessRiderMatchingResult(
            scheduledDeliveries.Count,
            expiredOffers.Count,
            attempts,
            created,
            noCandidates);
        telemetry.RecordProcessing(
            result.ScheduledDispatchesOpened,
            result.OffersExpired,
            result.MatchingAttempts,
            result.OffersCreated,
            result.NoCandidateAttempts);
        return result;
    }
}
