using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;

public sealed class ProcessRiderMatchingHandler(
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderMatchingPersistence persistence,
    IRiderMatchingTelemetry telemetry,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider,
    ILogger<ProcessRiderMatchingHandler> logger)
{
    public async Task<ProcessRiderMatchingResult> HandleAsync(
        ProcessRiderMatchingCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var settings = options.Value;
        var expiredOffers = await ScanExpiredOffersAsync(now, settings, cancellationToken);

        var expired = 0;
        var opened = 0;
        var attempts = 0;
        var created = 0;
        var noCandidates = 0;
        var failures = 0;

        foreach (var offer in expiredOffers)
        {
            try
            {
                expired += await ExpireOfferAsync(offer, now, settings, cancellationToken);
            }
            catch (Exception exception) when (IsIsolatedFailure(exception, cancellationToken))
            {
                failures++;
                LogIsolatedFailure(exception, "expire_offer", offer.OfferId);
            }
        }

        var scheduledDeliveryIds = await ScanScheduledDeliveriesAsync(now, settings, cancellationToken);
        foreach (var deliveryId in scheduledDeliveryIds)
        {
            try
            {
                opened += await OpenScheduledDeliveryAsync(deliveryId, now, cancellationToken);
            }
            catch (Exception exception) when (IsIsolatedFailure(exception, cancellationToken))
            {
                failures++;
                LogIsolatedFailure(exception, "open_scheduled_dispatch", deliveryId);
            }
        }

        var matchingDeliveryIds = await ScanMatchingDeliveriesAsync(now, settings, cancellationToken);
        foreach (var deliveryId in matchingDeliveryIds)
        {
            try
            {
                var unit = await MatchDeliveryAsync(deliveryId, now, settings, cancellationToken);
                attempts += unit.Attempts;
                created += unit.Created;
                noCandidates += unit.NoCandidates;
            }
            catch (Exception exception) when (IsIsolatedFailure(exception, cancellationToken))
            {
                failures++;
                LogIsolatedFailure(exception, "match_delivery", deliveryId);
            }
        }

        var result = new ProcessRiderMatchingResult(
            opened,
            expired,
            attempts,
            created,
            noCandidates,
            failures);
        telemetry.RecordProcessing(
            result.ScheduledDispatchesOpened,
            result.OffersExpired,
            result.MatchingAttempts,
            result.OffersCreated,
            result.NoCandidateAttempts);
        telemetry.RecordProcessingFailures(result.FailedOperations);
        return result;
    }

    private Task<IReadOnlyList<ExpiredRiderOfferCandidate>> ScanExpiredOffersAsync(
        DateTimeOffset now,
        RiderMatchingOptions settings,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(ScanExpiredOffersAsync)),
            async (transaction, token) =>
            {
                var candidates = await persistence.ListExpiredOfferCandidatesAsync(
                    now,
                    settings.MatchingScanBatchSize,
                    token);
                await transaction.CommitAsync(token);
                return candidates;
            },
            cancellationToken);

    private Task<IReadOnlyList<Guid>> ScanScheduledDeliveriesAsync(
        DateTimeOffset now,
        RiderMatchingOptions settings,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(ScanScheduledDeliveriesAsync)),
            async (transaction, token) =>
            {
                var ids = await persistence.ListScheduledDeliveryIdsAsync(
                    now,
                    settings.ScheduledDispatchScanBatchSize,
                    token);
                await transaction.CommitAsync(token);
                return ids;
            },
            cancellationToken);

    private Task<IReadOnlyList<Guid>> ScanMatchingDeliveriesAsync(
        DateTimeOffset now,
        RiderMatchingOptions settings,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(ScanMatchingDeliveriesAsync)),
            async (transaction, token) =>
            {
                var ids = await persistence.ListMatchingEligibleDeliveryIdsAsync(
                    now,
                    settings.MatchingScanBatchSize,
                    token);
                await transaction.CommitAsync(token);
                return ids;
            },
            cancellationToken);

    private Task<int> ExpireOfferAsync(
        ExpiredRiderOfferCandidate candidate,
        DateTimeOffset now,
        RiderMatchingOptions settings,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(ExpireOfferAsync), candidate.OfferId),
            async (transaction, token) =>
            {
                await consistencyLock.AcquireAssignmentAsync(
                    candidate.DeliveryId,
                    candidate.RiderStakeholderId,
                    token);
                var state = await persistence.LoadOfferExpiryStateAsync(candidate.OfferId, token);
                if (state.Offer is null || state.Offer.Status != RiderDeliveryOfferStatus.Pending ||
                    state.Offer.ExpiresAtUtc > now)
                {
                    await transaction.CommitAsync(token);
                    return 0;
                }

                RiderOfferRelease.Expire(state.Offer, state.MatchingSession, now, settings);

                await transaction.CommitAsync(token);
                return 1;
            },
            cancellationToken);

    private Task<int> OpenScheduledDeliveryAsync(
        Guid deliveryId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(OpenScheduledDeliveryAsync), deliveryId),
            async (transaction, token) =>
            {
                await consistencyLock.AcquireDeliveryAsync(deliveryId, token);
                var delivery = await persistence.LoadScheduledDeliveryAsync(deliveryId, now, token);
                if (delivery is null)
                {
                    await transaction.CommitAsync(token);
                    return 0;
                }

                delivery.OpenDispatch(now, "system", "scheduled_dispatch_job");
                persistence.AddStatusHistory(delivery.StatusHistory.Last());
                await transaction.CommitAsync(token);
                return 1;
            },
            cancellationToken);

    private Task<MatchingUnitResult> MatchDeliveryAsync(
        Guid deliveryId,
        DateTimeOffset now,
        RiderMatchingOptions settings,
        CancellationToken cancellationToken) =>
        transactionFactory.ExecuteAsync<MatchingUnitResult>(
            // Read committed so every read after an advisory lock observes the lock holder's committed changes;
            // a serializable snapshot would be taken before the lock wait completes.
            RiderTransactionIsolation.ReadCommitted,
            new(nameof(MatchDeliveryAsync), deliveryId),
            async (transaction, token) =>
            {
                // Another worker or a rider operation owns this delivery; it is rescanned next iteration.
                if (!await consistencyLock.TryAcquireDeliveryAsync(deliveryId, token))
                {
                    return default;
                }

                var state = await persistence.LoadMatchingStateAsync(deliveryId, token);
                var delivery = state.Delivery;
                if (delivery is null || !delivery.IsMatchingEligible)
                {
                    await transaction.CommitAsync(token);
                    return default;
                }

                var session = state.ActiveSession;
                if (session is null)
                {
                    session = DeliveryMatchingSession.Create(delivery.Id, settings.InitialSearchRadiusMeters, now);
                    persistence.AddMatchingSession(session);
                }

                if (session.NextAttemptAtUtc > now || state.HasPendingOffer)
                {
                    await transaction.CommitAsync(token);
                    return default;
                }

                var freshAfter = now.ToUniversalTime() - settings.LocationFreshness;
                var candidate = await persistence.FindCandidateAsync(
                    delivery,
                    session.SearchRadiusMeters,
                    freshAfter,
                    now,
                    settings.MaximumCandidatesPerRound,
                    token);
                var nextRadius = Math.Min(
                    settings.MaximumSearchRadiusMeters,
                    session.SearchRadiusMeters + settings.SearchRadiusExpansionMeters);
                var cycleBoundary = (session.AttemptCount + 1) % settings.MaximumAttemptsPerCycle == 0;

                if (candidate is not null)
                {
                    await consistencyLock.AcquireRiderAsync(candidate.RiderStakeholderId, token);
                    if (!await persistence.IsCandidateEligibleAsync(
                            delivery,
                            candidate.RiderProfileId,
                            session.SearchRadiusMeters,
                            freshAfter,
                            now,
                            token))
                    {
                        candidate = null;
                    }
                }

                session.RecordAttempt(
                    now,
                    candidate is null || cycleBoundary ? now + settings.RetryBackoff : now,
                    cycleBoundary ? settings.InitialSearchRadiusMeters : nextRadius);
                if (candidate is null)
                {
                    await transaction.CommitAsync(token);
                    return new(1, 0, 1);
                }

                persistence.AddOffer(RiderDeliveryOffer.Create(
                    session.Id,
                    delivery.Id,
                    candidate.RiderProfileId,
                    now,
                    now + settings.OfferTimeout));
                await transaction.CommitAsync(token);
                return new(1, 1, 0);
            },
            cancellationToken);

    private static bool IsIsolatedFailure(Exception exception, CancellationToken cancellationToken) =>
        !(exception is OperationCanceledException && cancellationToken.IsCancellationRequested);

    private void LogIsolatedFailure(Exception exception, string operation, Guid resourceId)
    {
        if (exception is RiderPersistenceConflictException or
            RiderTransientPersistenceException or
            RiderCommitOutcomeUnknownException)
        {
            logger.LogWarning(
                exception,
                "Rider matching operation {Operation} for resource {ResourceId} was deferred to a later iteration.",
                operation,
                resourceId);
            return;
        }

        logger.LogError(
            exception,
            "Rider matching operation {Operation} for resource {ResourceId} failed unexpectedly; continuing with the remaining batch.",
            operation,
            resourceId);
    }

    private readonly record struct MatchingUnitResult(int Attempts, int Created, int NoCandidates);
}
