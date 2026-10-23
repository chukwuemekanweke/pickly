using BLVK.Pickly.Logistics.Application.Operations;
using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Operations.GetOperationsTimeSeries;

public sealed class When_GettingOperationsTimeSeries_WithRecordedActivity_Should
{
    [Fact]
    public async Task MapCountsOntoTheirDays()
    {
        var from = new DateOnly(2026, 10, 20);
        var middle = new DateOnly(2026, 10, 21);
        var to = new DateOnly(2026, 10, 22);
        var reader = Substitute.For<IOperationsMetricsReader>();
        reader.GetDeliveryTransitionDailyCountsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<IReadOnlyCollection<DeliveryStatus>>(),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                new DeliveryTransitionDailyCount(from, DeliveryStatus.Delivered, 5, 3),
                new DeliveryTransitionDailyCount(from, DeliveryStatus.RiderAssigned, 6, 4),
                new DeliveryTransitionDailyCount(to, DeliveryStatus.RiderAssigned, 1, 1)
            ]);
        reader.GetScheduledPickupDailyCountsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns([new ScheduledPickupDailyCount(middle, 8)]);
        reader.GetCompletedDeliveryRevenueDailyTotalsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns([new DeliveryRevenueDailyTotal(from, "NGN", 7250.00m)]);
        var sut = new GetOperationsTimeSeriesHandler(
            reader,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero)));

        var result = await sut.HandleAsync(new(from, to), CancellationToken.None);

        var days = result.TimeSeries.ShouldNotBeNull().Days;
        days.Select(day => (day.Date, day.CompletedDeliveries, day.ScheduledDeliveries, day.RidersCompletingDeliveries, day.RidersAssignedDeliveries))
            .ShouldBe([(from, 5, 0, 3, 4), (middle, 0, 8, 0, 0), (to, 0, 0, 0, 1)]);
        days[0].Revenue.ShouldBe([new RevenueAmountResponse("NGN", 7250.00m)]);
        days[1].Revenue.ShouldBeEmpty();
        days[2].Revenue.ShouldBeEmpty();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
