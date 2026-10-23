using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Operations.GetOperationsTimeSeries;

public sealed class When_GettingOperationsTimeSeries_WithDefaultRange_Should
{
    [Fact]
    public async Task ReturnFourteenZeroFilledDaysEndingToday()
    {
        var now = new DateTimeOffset(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
        var fromUtc = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
        var toUtc = new DateTimeOffset(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
        var reader = Substitute.For<IOperationsMetricsReader>();
        reader.GetDeliveryTransitionDailyCountsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<IReadOnlyCollection<DeliveryStatus>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        reader.GetScheduledPickupDailyCountsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = new GetOperationsTimeSeriesHandler(reader, new FixedTimeProvider(now));

        var result = await sut.HandleAsync(new(From: null, To: null), CancellationToken.None);

        result.Status.ShouldBe(GetOperationsTimeSeriesStatus.Success);
        var series = result.TimeSeries.ShouldNotBeNull();
        series.From.ShouldBe(new DateOnly(2026, 10, 10));
        series.To.ShouldBe(new DateOnly(2026, 10, 23));
        series.Days.Count.ShouldBe(GetOperationsTimeSeriesHandler.DefaultDays);
        series.Days.ShouldAllBe(day =>
            day.CompletedDeliveries == 0 &&
            day.ScheduledDeliveries == 0 &&
            day.RidersCompletingDeliveries == 0 &&
            day.RidersAssignedDeliveries == 0);
        await reader.Received(1).GetDeliveryTransitionDailyCountsAsync(
            fromUtc,
            toUtc,
            Arg.Is<IReadOnlyCollection<DeliveryStatus>>(statuses =>
                statuses.Count == 2 &&
                statuses.Contains(DeliveryStatus.Delivered) &&
                statuses.Contains(DeliveryStatus.RiderAssigned)),
            Arg.Any<CancellationToken>());
        await reader.Received(1).GetScheduledPickupDailyCountsAsync(fromUtc, toUtc, Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
