using BLVK.Pickly.Logistics.Application.Authentication.Features.LogoutSession;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenLoggingOutWithValidToken_Should
{
    [Fact]
    public async Task RevokeAccessToken()
    {
        var context = new AuthenticationFlowTestContext();
        var tokenId = Guid.CreateVersion7().ToString("N");
        var stakeholderId = Guid.CreateVersion7();
        var expiresAtUtc = context.Clock.GetUtcNow().AddMinutes(5);
        var sessionId = Guid.CreateVersion7();
        context.SessionService.RevokeAsync(sessionId, stakeholderId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await context.CreateLogoutSessionHandler().HandleAsync(
            new LogoutSessionCommand(tokenId, expiresAtUtc, sessionId, stakeholderId,
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(LogoutSessionStatus.Success);
        await context.AccessTokenRevocationService.Received(1).RevokeAsync(tokenId, expiresAtUtc, Arg.Any<CancellationToken>());
        await context.SessionService.Received(1).RevokeAsync(sessionId, stakeholderId, Arg.Any<CancellationToken>());
    }
}

