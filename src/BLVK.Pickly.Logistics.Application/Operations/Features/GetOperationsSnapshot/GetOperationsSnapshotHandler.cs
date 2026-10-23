using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;

public sealed class GetOperationsSnapshotHandler(
    IOperationsMetricsReader reader,
    TimeProvider timeProvider)
{
    private static readonly DeliveryStatus[] DayOutcomeStatuses =
        [DeliveryStatus.Delivered, DeliveryStatus.Cancelled, DeliveryStatus.DeliveryFailed];

    public async Task<GetOperationsSnapshotResult> HandleAsync(
        GetOperationsSnapshotQuery query,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var date = query.Date ?? today;
        if (date > today)
        {
            return new(GetOperationsSnapshotStatus.DateInFuture);
        }

        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var isDayComplete = dayEnd <= now;
        // Inclusive state boundary: the last representable instant of a past day, or now for the current day.
        var asOf = isDayComplete ? dayEnd.AddTicks(-1) : now;

        var deliveryStates = await reader.GetDeliveryStatusCountsAsync(asOf, cancellationToken);
        var riderStates = await reader.GetRiderAvailabilityCountsAsync(asOf, cancellationToken);
        var outcomes = await reader.GetDeliveryTransitionDailyCountsAsync(
            dayStart,
            dayEnd,
            DayOutcomeStatuses,
            cancellationToken);
        var revenue = await reader.GetCompletedDeliveryRevenueDailyTotalsAsync(dayStart, dayEnd, cancellationToken);

        return new(
            GetOperationsSnapshotStatus.Success,
            new(
                date,
                asOf,
                isDayComplete,
                CountRiders(riderStates),
                CountDeliveries(deliveryStates, outcomes),
                revenue
                    .OrderBy(total => total.CurrencyCode, StringComparer.Ordinal)
                    .Select(total => new RevenueAmountResponse(total.CurrencyCode, total.Amount))
                    .ToArray()));
    }

    private static RiderCountsResponse CountRiders(IReadOnlyList<RiderAvailabilityCount> states)
    {
        int available = 0, busy = 0, unavailable = 0;
        foreach (var state in states)
        {
            // An active delivery makes the rider busy regardless of the persisted availability intent.
            if (state.HasActiveDelivery)
            {
                busy += state.Count;
            }
            else if (state.AvailabilityStatus == RiderAvailabilityStatus.Available)
            {
                available += state.Count;
            }
            else
            {
                unavailable += state.Count;
            }
        }

        return new(available, busy, unavailable, available + busy + unavailable);
    }

    private static DeliveryCountsResponse CountDeliveries(
        IReadOnlyList<DeliveryStatusCount> states,
        IReadOnlyList<DeliveryTransitionDailyCount> outcomes)
    {
        int State(params DeliveryStatus[] statuses) =>
            states.Where(state => statuses.Contains(state.Status)).Sum(state => state.Count);
        int Outcome(DeliveryStatus status) =>
            outcomes.Where(outcome => outcome.Status == status).Sum(outcome => outcome.DeliveryCount);

        return new(
            Pending: State(DeliveryStatus.AwaitingDispatch, DeliveryStatus.PendingAssignment),
            InProgress: State(DeliveryStatus.RiderAssigned, DeliveryStatus.PickedUp, DeliveryStatus.InTransit),
            AwaitingPayment: State(DeliveryStatus.AwaitingPayment),
            Completed: Outcome(DeliveryStatus.Delivered),
            Cancelled: Outcome(DeliveryStatus.Cancelled),
            Failed: Outcome(DeliveryStatus.DeliveryFailed));
    }
}
