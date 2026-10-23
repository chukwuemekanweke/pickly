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
        var sut = new GetOperationsTimeSeriesHandler(
            reader,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero)));

        var result = await sut.HandleAsync(new(from, to), CancellationToken.None);

        result.TimeSeries.ShouldNotBeNull().Days.ShouldBe(
        [
            new OperationsDailyPointResponse(from, 5, 0, 3, 4),
            new OperationsDailyPointResponse(middle, 0, 8, 0, 0),
            new OperationsDailyPointResponse(to, 0, 0, 0, 1)
        ]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
