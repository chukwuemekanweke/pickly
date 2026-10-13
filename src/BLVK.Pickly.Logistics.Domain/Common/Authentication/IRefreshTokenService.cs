using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IRefreshTokenService
{
    DateTimeOffset GetExpiry(TimeSpan? lifetime = null);
    Task<RefreshToken> IssueAsync(AppUser user, Guid sessionId, CancellationToken cancellationToken);
    Task<RefreshToken> IssueAsync(AppUser user, Guid sessionId, TimeSpan lifetime, CancellationToken cancellationToken);
    Task<RefreshToken> IssueAsync(AppUser user, Guid sessionId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<AuthenticationRefreshToken?> FindByTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken> RotateAsync(AuthenticationRefreshToken currentRefreshToken, AppUser user, CancellationToken cancellationToken);
    void Revoke(AuthenticationRefreshToken refreshToken, DateTimeOffset utcNow);
}
