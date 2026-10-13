using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Authentication.Sessions;

public sealed class When_RevokingSession_WithRepeatedRequest_Should
{
    [Fact]
    public void KeepOriginalRevocationTime()
    {
        var now = new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);
        var firstIpId = Guid.CreateVersion7();
        var session = AuthenticationSession.Create(Guid.CreateVersion7(),
            firstIpId, "Browser", null, null, null, now, now.AddDays(7));

        session.Revoke(now.AddHours(1));
        session.Revoke(now.AddHours(2));

        session.RevokedAtUtc.ShouldBe(now.AddHours(1));
        session.IsActive(now.AddHours(2)).ShouldBeFalse();
        session.FirstIpAddressId.ShouldBe(firstIpId);
    }
}
