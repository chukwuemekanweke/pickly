using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

/// <summary>
/// Set-based aggregate queries for operations metrics. Point-in-time state is reconstructed from append-only
/// history: a delivery's status at T is its latest <c>DeliveryStatusHistory</c> entry at or before T; a rider's
/// availability at T is its latest <c>RiderAvailabilityHistory</c> entry at or before T, otherwise the earliest later
/// entry's previous status (Offline when absent), otherwise the profile's current value (unchanged since tracking began).
/// </summary>
public sealed class OperationsMetricsReader(AppReadDbContext dbContext) : IOperationsMetricsReader
{
    private const int ActiveAssignmentStatusLow = (int)DeliveryStatus.RiderAssigned;
    private const int ActiveAssignmentStatusHigh = (int)DeliveryStatus.InTransit;
    private const int OfflineStatus = (int)RiderAvailabilityStatus.Offline;

    public async Task<IReadOnlyList<DeliveryStatusCount>> GetDeliveryStatusCountsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        var asOf = asOfUtc.ToUniversalTime();
        var rows = await dbContext.Database.SqlQuery<StatusCountRow>($"""
            SELECT state."NewStatus" AS "Status", COUNT(*)::int AS "Count"
            FROM (
                SELECT DISTINCT ON (h."DeliveryId") h."DeliveryId", h."NewStatus"
                FROM deliveries."DeliveryStatusHistory" h
                WHERE h."IsDeleted" = FALSE AND h."OccurredAtUtc" <= {asOf}
                ORDER BY h."DeliveryId", h."OccurredAtUtc" DESC, h."Id" DESC
            ) state
            JOIN deliveries."Deliveries" d ON d."Id" = state."DeliveryId" AND d."IsDeleted" = FALSE
            GROUP BY state."NewStatus"
            """).ToListAsync(cancellationToken);
        return rows.Select(row => new DeliveryStatusCount((DeliveryStatus)row.Status, row.Count)).ToArray();
    }

    public async Task<IReadOnlyList<RiderAvailabilityCount>> GetRiderAvailabilityCountsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        var asOf = asOfUtc.ToUniversalTime();
        var rows = await dbContext.Database.SqlQuery<RiderCountRow>($"""
            WITH delivery_state AS (
                SELECT DISTINCT ON (h."DeliveryId") h."DeliveryId", h."NewStatus"
                FROM deliveries."DeliveryStatusHistory" h
                WHERE h."IsDeleted" = FALSE AND h."OccurredAtUtc" <= {asOf}
                ORDER BY h."DeliveryId", h."OccurredAtUtc" DESC, h."Id" DESC
            ),
            busy AS (
                SELECT DISTINCT d."RiderId"
                FROM delivery_state state
                JOIN deliveries."Deliveries" d ON d."Id" = state."DeliveryId"
                WHERE d."IsDeleted" = FALSE
                  AND d."RiderId" IS NOT NULL
                  AND state."NewStatus" BETWEEN {ActiveAssignmentStatusLow} AND {ActiveAssignmentStatusHigh}
            ),
            rider_state AS (
                SELECT
                    COALESCE(
                        (SELECT h."NewStatus"
                         FROM riders."RiderAvailabilityHistory" h
                         WHERE h."RiderProfileId" = p."Id" AND h."IsDeleted" = FALSE AND h."OccurredAtUtc" <= {asOf}
                         ORDER BY h."OccurredAtUtc" DESC, h."Id" DESC
                         LIMIT 1),
                        (SELECT COALESCE(h."PreviousStatus", {OfflineStatus})
                         FROM riders."RiderAvailabilityHistory" h
                         WHERE h."RiderProfileId" = p."Id" AND h."IsDeleted" = FALSE AND h."OccurredAtUtc" > {asOf}
                         ORDER BY h."OccurredAtUtc", h."Id"
                         LIMIT 1),
                        p."AvailabilityStatus") AS "AvailabilityStatus",
                    (busy."RiderId" IS NOT NULL) AS "HasActiveDelivery"
                FROM riders."RiderProfiles" p
                LEFT JOIN busy ON busy."RiderId" = p."StakeholderId"
                WHERE p."IsDeleted" = FALSE AND p."CreatedAtUtc" <= {asOf}
            )
            SELECT "AvailabilityStatus", "HasActiveDelivery", COUNT(*)::int AS "Count"
            FROM rider_state
            GROUP BY "AvailabilityStatus", "HasActiveDelivery"
            """).ToListAsync(cancellationToken);
        return rows
            .Select(row => new RiderAvailabilityCount(
                (RiderAvailabilityStatus)row.AvailabilityStatus,
                row.HasActiveDelivery,
                row.Count))
            .ToArray();
    }

    public async Task<IReadOnlyList<DeliveryTransitionDailyCount>> GetDeliveryTransitionDailyCountsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyCollection<DeliveryStatus> statuses,
        CancellationToken cancellationToken)
    {
        if (statuses.Count == 0)
        {
            return [];
        }

        var from = fromUtc.ToUniversalTime();
        var to = toUtc.ToUniversalTime();
        var statusValues = statuses.Select(status => (int)status).Distinct().ToArray();
        var rows = await dbContext.Database.SqlQuery<TransitionRow>($"""
            SELECT
                (h."OccurredAtUtc" AT TIME ZONE 'UTC')::date AS "Day",
                h."NewStatus" AS "Status",
                COUNT(DISTINCT h."DeliveryId")::int AS "DeliveryCount",
                COUNT(DISTINCT d."RiderId")::int AS "RiderCount"
            FROM deliveries."DeliveryStatusHistory" h
            JOIN deliveries."Deliveries" d ON d."Id" = h."DeliveryId" AND d."IsDeleted" = FALSE
            WHERE h."IsDeleted" = FALSE
              AND h."OccurredAtUtc" >= {from}
              AND h."OccurredAtUtc" < {to}
              AND h."NewStatus" = ANY({statusValues})
            GROUP BY 1, 2
            """).ToListAsync(cancellationToken);
        return rows
            .Select(row => new DeliveryTransitionDailyCount(row.Day, (DeliveryStatus)row.Status, row.DeliveryCount, row.RiderCount))
            .ToArray();
    }

    public async Task<IReadOnlyList<DeliveryRevenueDailyTotal>> GetCompletedDeliveryRevenueDailyTotalsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var from = fromUtc.ToUniversalTime();
        var to = toUtc.ToUniversalTime();
        var delivered = (int)DeliveryStatus.Delivered;
        // One row per delivery (its first completion in range) so a duplicated history entry cannot double-count.
        var rows = await dbContext.Database.SqlQuery<RevenueRow>($"""
            SELECT completed."Day", c."CurrencyCode", SUM(d."ChargeAmount") AS "Amount"
            FROM (
                SELECT DISTINCT ON (h."DeliveryId")
                    h."DeliveryId", (h."OccurredAtUtc" AT TIME ZONE 'UTC')::date AS "Day"
                FROM deliveries."DeliveryStatusHistory" h
                WHERE h."IsDeleted" = FALSE
                  AND h."OccurredAtUtc" >= {from}
                  AND h."OccurredAtUtc" < {to}
                  AND h."NewStatus" = {delivered}
                ORDER BY h."DeliveryId", h."OccurredAtUtc", h."Id"
            ) completed
            JOIN deliveries."Deliveries" d ON d."Id" = completed."DeliveryId" AND d."IsDeleted" = FALSE
            JOIN payments."Currencies" c ON c."Id" = d."CurrencyId"
            GROUP BY completed."Day", c."CurrencyCode"
            """).ToListAsync(cancellationToken);
        return rows.Select(row => new DeliveryRevenueDailyTotal(row.Day, row.CurrencyCode, row.Amount)).ToArray();
    }

    public async Task<IReadOnlyList<ScheduledPickupDailyCount>> GetScheduledPickupDailyCountsAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var from = fromUtc.ToUniversalTime();
        var to = toUtc.ToUniversalTime();
        var scheduled = (int)DeliveryType.Scheduled;
        var rows = await dbContext.Database.SqlQuery<DayCountRow>($"""
            SELECT (d."ScheduledPickupAtUtc" AT TIME ZONE 'UTC')::date AS "Day", COUNT(*)::int AS "Count"
            FROM deliveries."Deliveries" d
            WHERE d."IsDeleted" = FALSE
              AND d."DeliveryType" = {scheduled}
              AND d."ScheduledPickupAtUtc" >= {from}
              AND d."ScheduledPickupAtUtc" < {to}
            GROUP BY 1
            """).ToListAsync(cancellationToken);
        return rows.Select(row => new ScheduledPickupDailyCount(row.Day, row.Count)).ToArray();
    }

    private sealed class StatusCountRow { public int Status { get; init; } public int Count { get; init; } }

    private sealed class RiderCountRow { public int AvailabilityStatus { get; init; } public bool HasActiveDelivery { get; init; } public int Count { get; init; } }

    private sealed class TransitionRow { public DateOnly Day { get; init; } public int Status { get; init; } public int DeliveryCount { get; init; } public int RiderCount { get; init; } }

    private sealed class RevenueRow { public DateOnly Day { get; init; } public string CurrencyCode { get; init; } = string.Empty; public decimal Amount { get; init; } }

    private sealed class DayCountRow { public DateOnly Day { get; init; } public int Count { get; init; } }
}
