namespace BLVK.Pickly.Logistics.Domain.Common.Caching;

/// <summary>A bounded cross-instance lease. The returned owner token must be supplied to release it.</summary>
public interface IDistributedLease
{
    Task<string?> TryAcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken);
    Task ReleaseAsync(string key, string ownerToken, CancellationToken cancellationToken);
}
