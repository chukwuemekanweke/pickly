using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class ActiveSessionAuthorizationHandler(
    IAccessTokenRevocationService accessTokenRevocationService,
    IAuthenticationSessionService sessionService,
    TimeProvider timeProvider)
    : AuthorizationHandler<ActiveSessionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSessionRequirement requirement)
    {
        var tokenId = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return;
        }

        if (await accessTokenRevocationService.IsRevokedAsync(tokenId, CancellationToken.None))
        {
            return;
        }

        if (!Guid.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value ??
                context.User.FindFirst(ClaimTypes.Sid)?.Value, out var sessionId))
        {
            return;
        }

        var session = await sessionService.FindAsync(sessionId, CancellationToken.None);
        if (session is null || !session.IsActive(timeProvider.GetUtcNow()) ||
            context.User.FindFirst(CustomClaimTypes.StakeholderId)?.Value != session.StakeholderId.ToString())
        {
            return;
        }

        context.Succeed(requirement);
    }
}
