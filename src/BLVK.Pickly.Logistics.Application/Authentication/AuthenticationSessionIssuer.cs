using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed class AuthenticationSessionIssuer(
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthenticationSessionService sessionService)
{
    public async Task<AuthenticationTokens> IssueAsync(
        AppUser user,
        Stakeholder stakeholder,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var session = await sessionService.CreateAsync(
            stakeholder,
            ipAddress,
            userAgent,
            refreshTokenService.GetExpiry(),
            cancellationToken);
        var accessToken = accessTokenService.Generate(user, stakeholder.Id, session.Id);
        var refreshToken = await refreshTokenService.IssueAsync(
            user,
            session.Id,
            session.ExpiresAtUtc,
            cancellationToken);

        return new AuthenticationTokens(accessToken, refreshToken);
    }
}
