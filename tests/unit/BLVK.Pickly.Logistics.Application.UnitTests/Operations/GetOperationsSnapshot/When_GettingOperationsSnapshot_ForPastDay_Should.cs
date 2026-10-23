using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Operations.GetOperationsSnapshot;

public sealed class When_GettingOperationsSnapshot_ForPastDay_Should
{
    [Fact]
    public async Task GroupStateAtEndOfDayAndOutcomesDuringDay()
    {
        var now = new DateTimeOffset(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
        var date = new DateOnly(2026, 10, 20);
        var dayStart = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var endOfDay = dayStart.AddDays(1).AddTicks(-1);
        var reader = Substitute.For<IOperationsMetricsReader>();
        reader.GetDeliveryStatusCountsAsync(endOfDay, Arg.Any<CancellationToken>()).Returns(
        [
            new DeliveryStatusCount(DeliveryStatus.AwaitingPayment, 1),
            new DeliveryStatusCount(DeliveryStatus.AwaitingDispatch, 2),
            new DeliveryStatusCount(DeliveryStatus.PendingAssignment, 3),
            new DeliveryStatusCount(DeliveryStatus.RiderAssigned, 4),
            new DeliveryStatusCount(DeliveryStatus.PickedUp, 5),
            new DeliveryStatusCount(DeliveryStatus.InTransit, 6),
            new DeliveryStatusCount(DeliveryStatus.Delivered, 70)
        ]);
        reader.GetRiderAvailabilityCountsAsync(endOfDay, Arg.Any<CancellationToken>()).Returns(
        [
            new RiderAvailabilityCount(RiderAvailabilityStatus.Available, false, 7),
            new RiderAvailabilityCount(RiderAvailabilityStatus.Available, true, 2),
            new RiderAvailabilityCount(RiderAvailabilityStatus.Offline, true, 1),
            new RiderAvailabilityCount(RiderAvailabilityStatus.Offline, false, 4),
            new RiderAvailabilityCount(RiderAvailabilityStatus.Paused, false, 3)
        ]);
        reader.GetDeliveryTransitionDailyCountsAsync(
                dayStart,
                dayStart.AddDays(1),
                Arg.Is<IReadOnlyCollection<DeliveryStatus>>(statuses =>
                    statuses.Count == 3 &&
                    statuses.Contains(DeliveryStatus.Delivered) &&
                    statuses.Contains(DeliveryStatus.Cancelled) &&
                    statuses.Contains(DeliveryStatus.DeliveryFailed)),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                new DeliveryTransitionDailyCount(date, DeliveryStatus.Delivered, 9, 4),
                new DeliveryTransitionDailyCount(date, DeliveryStatus.Cancelled, 2, 0),
                new DeliveryTransitionDailyCount(date, DeliveryStatus.DeliveryFailed, 1, 1)
            ]);
        var sut = new GetOperationsSnapshotHandler(reader, new FixedTimeProvider(now));

        var result = await sut.HandleAsync(new(date), CancellationToken.None);

        result.Status.ShouldBe(GetOperationsSnapshotStatus.Success);
        var snapshot = result.Snapshot.ShouldNotBeNull();
        snapshot.Date.ShouldBe(date);
        snapshot.AsOfUtc.ShouldBe(endOfDay);
        snapshot.IsDayComplete.ShouldBeTrue();
        snapshot.Riders.ShouldBe(new RiderCountsResponse(Available: 7, Busy: 3, Unavailable: 7, Total: 17));
        snapshot.Deliveries.ShouldBe(new DeliveryCountsResponse(
            Pending: 5,
            InProgress: 15,
            AwaitingPayment: 1,
            Completed: 9,
            Cancelled: 2,
            Failed: 1));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
