using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Riders;

public sealed class When_UpdatingRiderLocation_Should
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RejectOutOfOrderHeartbeat()
    {
        var location = RiderCurrentLocation.Create(Guid.CreateVersion7(), Now, Now);

        location.TryAdvance(Now.AddSeconds(-1), Now.AddSeconds(1)).ShouldBeFalse();
        location.CapturedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void AdvanceOnlyForNewerHeartbeat()
    {
        var location = RiderCurrentLocation.Create(Guid.CreateVersion7(), Now, Now);

        location.TryAdvance(Now.AddSeconds(1), Now.AddSeconds(2)).ShouldBeTrue();
        location.CapturedAtUtc.ShouldBe(Now.AddSeconds(1));
        location.ReceivedAtUtc.ShouldBe(Now.AddSeconds(2));
    }
}
