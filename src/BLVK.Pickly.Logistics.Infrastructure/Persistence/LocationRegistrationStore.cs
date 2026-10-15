using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

/// <summary>
/// Registers provider-resolved locations in a short transaction. A transaction-scoped advisory lock keyed by the
/// provider feature serializes concurrent registrations of the same feature, and the unique provider-reference index
/// remains the final integrity safeguard. No network call ever happens while this transaction is open.
/// </summary>
public sealed class LocationRegistrationStore(AppDbContext dbContext) : ILocationRegistrationStore
{
    public async Task<LocationRegistration> RegisterAsync(
        Location candidate,
        string provider,
        string featureId,
        string? resolutionKey,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = LocationProviderReference.NormalizeProvider(provider);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var lockKey = $"location-provider-reference:{normalizedProvider}:{featureId}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);

        var existing = await dbContext.Locations
            .IgnoreQueryFilters()
            .Include(location => location.ProviderReferences)
            .Include(location => location.ResolutionKeys)
            .FirstOrDefaultAsync(
                location => location.ProviderReferences.Any(reference =>
                    reference.Provider == normalizedProvider && reference.FeatureId == featureId),
                cancellationToken);

        var target = existing ?? candidate;
        if (resolutionKey is not null && target.IsUsable && !await IsResolutionKeyTakenAsync(resolutionKey, cancellationToken))
        {
            target.AddResolutionKey(resolutionKey);
        }

        if (existing is null)
        {
            dbContext.Locations.Add(candidate);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LocationRegistration(target, existing is null);
    }

    private Task<bool> IsResolutionKeyTakenAsync(string resolutionKey, CancellationToken cancellationToken) =>
        dbContext.LocationResolutionKeys
            .IgnoreQueryFilters()
            .AnyAsync(key => key.Key == resolutionKey, cancellationToken);
}
