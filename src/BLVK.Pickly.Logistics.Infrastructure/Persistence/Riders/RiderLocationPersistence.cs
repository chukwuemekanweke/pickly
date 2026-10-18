using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.Infrastructure.Riders;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderLocationPersistence(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderLocationPersistence
{
    public async Task<RiderOperationResult> UpdateAsync(
        ActorContext actor,
        decimal latitude,
        decimal longitude,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        if (latitude is < -90m or > 90m || longitude is < -180m or > 180m)
        {
            return new(RiderOperationCode.InvalidLocation);
        }

        var captured = capturedAtUtc.ToUniversalTime();
        var now = utcNow.ToUniversalTime();
        if (captured > now + options.MaximumFutureLocationClockSkew)
        {
            return new(RiderOperationCode.InvalidLocation);
        }

        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var profile = resolved.Profile!;
        await support.AcquireRiderLockAsync(profile.StakeholderId, cancellationToken);
        await dbContext.Entry(profile).ReloadAsync(cancellationToken);
        if (!profile.IsApproved)
        {
            return new(RiderOperationCode.ProfileInactive);
        }

        var location = await dbContext.RiderCurrentLocations
            .SingleOrDefaultAsync(item => item.RiderProfileId == profile.Id, cancellationToken);
        var point = RiderPersistenceSupport.CreatePoint(latitude, longitude);

        if (location is null)
        {
            location = RiderCurrentLocation.Create(profile.Id, captured, now);
            dbContext.RiderCurrentLocations.Add(location);
            dbContext.Entry(location).Property<Point>(RiderCurrentLocationConfiguration.PositionProperty).CurrentValue = point;
        }
        else
        {
            if (!location.TryAdvance(captured, now))
            {
                RiderMatchingMetrics.RecordStaleLocation();
                return new(RiderOperationCode.StaleLocation);
            }

            dbContext.Entry(location).Property<Point>(RiderCurrentLocationConfiguration.PositionProperty).CurrentValue = point;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(RiderOperationCode.Success);
    }
}
