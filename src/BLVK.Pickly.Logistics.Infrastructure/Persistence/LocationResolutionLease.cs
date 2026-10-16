using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

/// <summary>
/// Database-backed lease shared by every API instance. Acquisition is a single autocommit upsert that only takes over
/// an expired lease, so no transaction or session lock is held while the caller talks to the geocoding provider, and
/// it works behind transaction-pooling connection proxies.
/// </summary>
public sealed class LocationResolutionLease(AppDbContext dbContext, TimeProvider timeProvider) : ILocationResolutionLease
{
    public async Task<Guid?> TryAcquireAsync(
        string resolutionKey,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var leaseId = Guid.CreateVersion7();
        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = now.Add(duration);

        var acquired = await dbContext.Database
            .SqlQuery<Guid>(
                $"""
                INSERT INTO locations."LocationResolutionLeases" ("ResolutionKey", "LeaseId", "ExpiresAtUtc")
                VALUES ({resolutionKey}, {leaseId}, {expiresAtUtc})
                ON CONFLICT ("ResolutionKey") DO UPDATE
                    SET "LeaseId" = EXCLUDED."LeaseId", "ExpiresAtUtc" = EXCLUDED."ExpiresAtUtc"
                    WHERE locations."LocationResolutionLeases"."ExpiresAtUtc" < {now}
                RETURNING "LeaseId" AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return acquired.Count == 1 ? acquired[0] : null;
    }

    public Task ReleaseAsync(string resolutionKey, Guid leaseId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM locations."LocationResolutionLeases"
            WHERE "ResolutionKey" = {resolutionKey} AND "LeaseId" = {leaseId}
            """,
            cancellationToken);
}
