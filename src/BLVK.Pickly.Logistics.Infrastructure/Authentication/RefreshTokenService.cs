using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class RefreshTokenService(
    IRepository<AuthenticationRefreshToken> refreshTokenRepository,
    IAuthenticationIdentityService identityService,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider) : IRefreshTokenService
{
    private readonly RefreshTokenOptions _options = options.Value;

    public DateTimeOffset GetExpiry(TimeSpan? lifetime = null) =>
        timeProvider.GetUtcNow().Add(lifetime ?? TimeSpan.FromDays(_options.LifetimeDays));

    public async Task<RefreshToken> IssueAsync(AppUser user, Guid sessionId, CancellationToken cancellationToken)
    {
        return await IssueAsync(user, sessionId, GetExpiry(), cancellationToken);
    }

    public async Task<RefreshToken> IssueAsync(
        AppUser user,
        Guid sessionId,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        return await IssueAsync(user, sessionId, GetExpiry(lifetime), cancellationToken);
    }

    public async Task<RefreshToken> IssueAsync(
        AppUser user,
        Guid sessionId,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var rawToken = GenerateRawToken();
        var securityStamp = await GetRequiredSecurityStampAsync(user, cancellationToken);
        var refreshToken = AuthenticationRefreshToken.Create(user.Id, sessionId, ComputeHash(rawToken), securityStamp, expiresAtUtc);

        await refreshTokenRepository.AddAsync(refreshToken);

        return new RefreshToken(rawToken, expiresAtUtc);
    }

    public Task<AuthenticationRefreshToken?> FindByTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
        refreshTokenRepository.FirstOrDefaultAsync(
            new RefreshTokenByHashSpecification(ComputeHash(refreshToken)),
            cancellationToken);

    public async Task<RefreshToken> RotateAsync(AuthenticationRefreshToken currentRefreshToken, AppUser user, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        Revoke(currentRefreshToken, utcNow);
        refreshTokenRepository.Update(currentRefreshToken);

        return await IssueAsync(user, currentRefreshToken.AuthenticationSessionId
            ?? throw new InvalidOperationException("Refresh token has no session."), currentRefreshToken.ExpiresAtUtc, cancellationToken);
    }

    public void Revoke(AuthenticationRefreshToken refreshToken, DateTimeOffset utcNow)
    {
        refreshToken.Revoke(utcNow);
        refreshTokenRepository.Update(refreshToken);
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHash(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<string> GetRequiredSecurityStampAsync(AppUser user, CancellationToken cancellationToken)
    {
        var securityStamp = await identityService.GetSecurityStampAsync(user);
        if (string.IsNullOrWhiteSpace(securityStamp))
        {
            throw new InvalidOperationException($"Security stamp is missing for user '{user.Id}'.");
        }

        return securityStamp;
    }
}

