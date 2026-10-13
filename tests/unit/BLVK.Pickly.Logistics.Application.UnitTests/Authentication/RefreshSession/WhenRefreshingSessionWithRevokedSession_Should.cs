using BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.RefreshSession;

public sealed class WhenRefreshingSessionWithRevokedSession_Should
{
    [Fact]
    public async Task RejectRefreshToken()
    {
        var context = new AuthenticationFlowTestContext();
        var now = context.Clock.GetUtcNow();
        var user = AppUser.Create(AuthenticationTestData.Email());
        var session = AuthenticationSession.Create(Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Browser", null, null, null, now, now.AddDays(1));
        session.Revoke(now);
        var refreshToken = AuthenticationRefreshToken.Create(user.Id, session.Id, "HASH", "stamp", now.AddDays(1));
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>()).Returns(refreshToken);
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await context.CreateRefreshSessionHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRefreshSessionCommand(), CancellationToken.None);

        result.Status.ShouldBe(RefreshSessionStatus.InvalidRefreshToken);
        await context.IdentityService.DidNotReceive().FindByIdAsync(Arg.Any<Guid>());
    }
}
