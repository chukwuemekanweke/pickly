using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Generate(AppUser user, Guid stakeholderId, Guid sessionId)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = now.AddMinutes(_options.LifetimeMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, sessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? user.UserName ?? string.Empty),
            new Claim(CustomClaimTypes.StakeholderId, stakeholderId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
