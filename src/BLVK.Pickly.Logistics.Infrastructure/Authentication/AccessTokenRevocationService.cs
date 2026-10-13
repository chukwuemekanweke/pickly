using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class AccessTokenRevocationService(IJsonCache cache) : IAccessTokenRevocationService
{
    public async Task RevokeAsync(string tokenId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var ttl = expiresAtUtc - DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(tokenId) || ttl <= TimeSpan.Zero)
        {
            return;
        }

        await cache.SetAsync(BuildCacheKey(tokenId), true, ttl, cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return false;
        }

        return await cache.GetAsync<bool>(BuildCacheKey(tokenId), cancellationToken);
    }

    private static string BuildCacheKey(string tokenId) => $"authentication:access-token:revoked:{tokenId}";
}
