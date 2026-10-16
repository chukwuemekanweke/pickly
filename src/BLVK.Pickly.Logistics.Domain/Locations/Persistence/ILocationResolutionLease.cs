namespace BLVK.Pickly.Logistics.Domain.Locations.Persistence;

/// <summary>
/// A short-lived, cross-instance lease that lets one request at a time call the paid geocoding provider for a given
/// resolution key. Acquisition is a single autocommit statement; no transaction is held during the provider call.
/// </summary>
public interface ILocationResolutionLease
{
    Task<Guid?> TryAcquireAsync(string resolutionKey, TimeSpan duration, CancellationToken cancellationToken);

    Task ReleaseAsync(string resolutionKey, Guid leaseId, CancellationToken cancellationToken);
}
