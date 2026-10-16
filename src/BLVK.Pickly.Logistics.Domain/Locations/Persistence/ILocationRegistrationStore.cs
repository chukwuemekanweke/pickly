using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Domain.Locations.Persistence;

/// <summary>
/// Persists a provider-resolved location in one short transaction, reusing an existing location that already
/// carries the same provider reference instead of inserting a duplicate.
/// </summary>
public interface ILocationRegistrationStore
{
    Task<LocationRegistration> RegisterAsync(
        Location candidate,
        string provider,
        string featureId,
        string? resolutionKey,
        CancellationToken cancellationToken);
}
