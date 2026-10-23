using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Operations.GetOperationsSnapshot;

public sealed class When_GettingOperationsSnapshot_ForCurrentDay_Should
{
    [Fact]
    public async Task UseCurrentInstantAsStateBoundary()
    {
        var now = new DateTimeOffset(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
        var dayStart = new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero);
        var reader = Substitute.For<IOperationsMetricsReader>();
        reader.GetDeliveryStatusCountsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        reader.GetRiderAvailabilityCountsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(
            [new RiderAvailabilityCount(RiderAvailabilityStatus.Available, false, 2)]);
        reader.GetDeliveryTransitionDailyCountsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<IReadOnlyCollection<DeliveryStatus>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        reader.GetCompletedDeliveryRevenueDailyTotalsAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = new GetOperationsSnapshotHandler(reader, new FixedTimeProvider(now));

        var result = await sut.HandleAsync(new(Date: null), CancellationToken.None);

        var snapshot = result.Snapshot.ShouldNotBeNull();
        snapshot.Date.ShouldBe(new DateOnly(2026, 10, 23));
        snapshot.AsOfUtc.ShouldBe(now);
        snapshot.IsDayComplete.ShouldBeFalse();
        snapshot.Riders.ShouldBe(new RiderCountsResponse(2, 0, 0, 2));
        snapshot.Deliveries.ShouldBe(new DeliveryCountsResponse(0, 0, 0, 0, 0, 0));
        snapshot.Revenue.ShouldBeEmpty();
        await reader.Received(1).GetDeliveryStatusCountsAsync(now, Arg.Any<CancellationToken>());
        await reader.Received(1).GetRiderAvailabilityCountsAsync(now, Arg.Any<CancellationToken>());
        await reader.Received(1).GetDeliveryTransitionDailyCountsAsync(
            dayStart,
            dayStart.AddDays(1),
            Arg.Any<IReadOnlyCollection<DeliveryStatus>>(),
            Arg.Any<CancellationToken>());
        await reader.Received(1).GetCompletedDeliveryRevenueDailyTotalsAsync(
            dayStart,
            dayStart.AddDays(1),
            Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
