using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenAuthorizingActiveSessionWithRevokedSession_Should
{
    [Fact]
    public async Task Fail()
    {
        var now = new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);
        var userId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var session = AuthenticationSession.Create(stakeholderId,
            Guid.CreateVersion7(), "Browser", null, null, null, now, now.AddDays(1));
        session.Revoke(now);
        var sessions = Substitute.For<IAuthenticationSessionService>();
        sessions.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var handler = new ActiveSessionAuthorizationHandler(
            Substitute.For<IAccessTokenRevocationService>(), sessions, new FixedTimeProvider(now));
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, session.Id.ToString()),
            new Claim(CustomClaimTypes.StakeholderId, stakeholderId.ToString())
        ], "Bearer"));
        var context = new AuthorizationHandlerContext([new ActiveSessionRequirement()], user, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
