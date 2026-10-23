using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;

public sealed class GetOperationsTimeSeriesHandler(
    IOperationsMetricsReader reader,
    TimeProvider timeProvider)
{
    public const int DefaultDays = 14;
    public const int MaximumDays = 366;

    private static readonly DeliveryStatus[] TrackedTransitions =
        [DeliveryStatus.Delivered, DeliveryStatus.RiderAssigned];

    public async Task<GetOperationsTimeSeriesResult> HandleAsync(
        GetOperationsTimeSeriesQuery query,
        CancellationToken cancellationToken)
    {
        var to = query.To ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = query.From ?? to.AddDays(-(DefaultDays - 1));
        var dayCount = to.DayNumber - from.DayNumber + 1;
        if (dayCount < 1 || dayCount > MaximumDays)
        {
            return new(GetOperationsTimeSeriesStatus.InvalidRange);
        }

        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = fromUtc.AddDays(dayCount);
        var transitions = await reader.GetDeliveryTransitionDailyCountsAsync(
            fromUtc,
            toUtc,
            TrackedTransitions,
            cancellationToken);
        var scheduled = await reader.GetScheduledPickupDailyCountsAsync(fromUtc, toUtc, cancellationToken);
        var revenue = await reader.GetCompletedDeliveryRevenueDailyTotalsAsync(fromUtc, toUtc, cancellationToken);

        var transitionsByDay = transitions.ToLookup(row => (row.Day, row.Status));
        var scheduledByDay = scheduled.ToDictionary(row => row.Day, row => row.Count);
        var revenueByDay = revenue.ToLookup(row => row.Day);
        var days = Enumerable.Range(0, dayCount)
            .Select(offset =>
            {
                var day = from.AddDays(offset);
                var completed = transitionsByDay[(day, DeliveryStatus.Delivered)].FirstOrDefault();
                var assigned = transitionsByDay[(day, DeliveryStatus.RiderAssigned)].FirstOrDefault();
                return new OperationsDailyPointResponse(
                    day,
                    completed?.DeliveryCount ?? 0,
                    scheduledByDay.GetValueOrDefault(day),
                    completed?.RiderCount ?? 0,
                    assigned?.RiderCount ?? 0,
                    revenueByDay[day]
                        .OrderBy(total => total.CurrencyCode, StringComparer.Ordinal)
                        .Select(total => new RevenueAmountResponse(total.CurrencyCode, total.Amount))
                        .ToArray());
            })
            .ToArray();

        return new(GetOperationsTimeSeriesStatus.Success, new(from, to, days));
    }
}
