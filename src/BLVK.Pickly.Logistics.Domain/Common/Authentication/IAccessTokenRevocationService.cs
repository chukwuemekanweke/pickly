namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IAccessTokenRevocationService
{
    Task RevokeAsync(string tokenId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken);
}
