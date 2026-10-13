using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Authentication.Sessions;

public sealed class When_TouchingSession_WithNewIp_Should
{
    [Fact]
    public void UpdateLastActivityWithoutChangingFirstIp()
    {
        var now = new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);
        var firstIpId = Guid.CreateVersion7();
        var nextIpId = Guid.CreateVersion7();
        var session = AuthenticationSession.Create(Guid.CreateVersion7(),
            firstIpId, "Old browser", null, null, null, now, now.AddDays(7));

        session.Touch(now.AddHours(1), nextIpId, "New browser", "Phone", "Android", "Chrome");

        session.LastActiveAtUtc.ShouldBe(now.AddHours(1));
        session.FirstIpAddressId.ShouldBe(firstIpId);
        session.LastIpAddressId.ShouldBe(nextIpId);
        session.DeviceName.ShouldBe("Phone");
    }
}
