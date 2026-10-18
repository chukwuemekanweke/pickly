using BLVK.Pickly.Logistics.Domain.Riders;
using System.Diagnostics.Metrics;
using DomainObservability = BLVK.Pickly.Logistics.Domain.Common.Observability.Observability;

namespace BLVK.Pickly.Logistics.Infrastructure.Riders;

internal sealed class RiderMatchingMetrics : IRiderMatchingTelemetry
{
    private static readonly Meter Meter = new(DomainObservability.MeterName);
    private static readonly Counter<long> ScheduledDispatchesOpened = Meter.CreateCounter<long>("pickly.riders.scheduled_dispatches.opened");
    private static readonly Counter<long> MatchingAttempts = Meter.CreateCounter<long>("pickly.riders.matching.attempts");
    private static readonly Counter<long> NoCandidateAttempts = Meter.CreateCounter<long>("pickly.riders.matching.no_candidate");
    private static readonly Counter<long> OffersCreated = Meter.CreateCounter<long>("pickly.riders.offers.created");
    private static readonly Counter<long> OffersExpired = Meter.CreateCounter<long>("pickly.riders.offers.expired");
    private static readonly Counter<long> OffersAccepted = Meter.CreateCounter<long>("pickly.riders.offers.accepted");
    private static readonly Counter<long> OffersRejected = Meter.CreateCounter<long>("pickly.riders.offers.rejected");
    private static readonly Counter<long> AssignmentConflicts = Meter.CreateCounter<long>("pickly.riders.assignments.conflicts");
    private static readonly Counter<long> StaleLocations = Meter.CreateCounter<long>("pickly.riders.locations.stale");
    private static readonly Counter<long> ProcessingFailures = Meter.CreateCounter<long>("pickly.riders.matching.processing_failures");

    public void RecordProcessing(
        int scheduledDispatchesOpened,
        int offersExpired,
        int matchingAttempts,
        int offersCreated,
        int noCandidateAttempts)
    {
        ScheduledDispatchesOpened.Add(scheduledDispatchesOpened);
        MatchingAttempts.Add(matchingAttempts);
        NoCandidateAttempts.Add(noCandidateAttempts);
        OffersCreated.Add(offersCreated);
        OffersExpired.Add(offersExpired);
    }

    public void RecordOfferAccepted() => OffersAccepted.Add(1);
    public void RecordOfferRejected() => OffersRejected.Add(1);
    public void RecordAssignmentConflict() => AssignmentConflicts.Add(1);
    public void RecordStaleLocation() => StaleLocations.Add(1);
    public void RecordProcessingFailures(int count) => ProcessingFailures.Add(count);
}
