using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics;

internal static class OperationsMetricsControllerFactory
{
    public static readonly DateTimeOffset Now = new(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);

    public static OperationsMetricsController Create(IOperationsMetricsReader reader)
    {
        var clock = new FixedTimeProvider(Now);
        return new(
            new GetOperationsSnapshotHandler(reader, clock),
            new GetOperationsTimeSeriesHandler(reader, clock),
            new OperationsSnapshotRequestValidator(),
            new OperationsTimeSeriesRequestValidator());
    }

    public static IOperationsMetricsReader EmptyReader()
    {
        var reader = Substitute.For<IOperationsMetricsReader>();
        reader.GetDeliveryStatusCountsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        reader.GetRiderAvailabilityCountsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
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
        return reader;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
