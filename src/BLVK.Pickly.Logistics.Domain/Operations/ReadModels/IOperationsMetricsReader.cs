using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.Operations.ReadModels;

/// <summary>
/// Bounded aggregate queries over delivery status history, rider availability history and scheduled deliveries.
/// Returns factual counts only; grouping into operational categories belongs to the calling handler.
/// </summary>
public interface IOperationsMetricsReader
{
    /// <summary>Counts deliveries by the status each held at <paramref name="asOfUtc"/>.</summary>
    Task<IReadOnlyList<DeliveryStatusCount>> GetDeliveryStatusCountsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    /// <summary>Counts riders that existed at <paramref name="asOfUtc"/> by their availability and active-delivery state.</summary>
    Task<IReadOnlyList<RiderAvailabilityCount>> GetRiderAvailabilityCountsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    /// <summary>Counts transitions into <paramref name="statuses"/> per UTC day within [from, to).</summary>
    Task<IReadOnlyList<DeliveryTransitionDailyCount>> GetDeliveryTransitionDailyCountsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyCollection<DeliveryStatus> statuses,
        CancellationToken cancellationToken);

    /// <summary>Counts scheduled deliveries per UTC pickup day within [from, to).</summary>
    Task<IReadOnlyList<ScheduledPickupDailyCount>> GetScheduledPickupDailyCountsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken);
}
