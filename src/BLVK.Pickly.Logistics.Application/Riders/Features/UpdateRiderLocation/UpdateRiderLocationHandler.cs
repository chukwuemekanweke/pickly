using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;

public sealed class UpdateRiderLocationHandler(
    RiderActorResolver actorResolver,
    IRiderTransactionFactory transactionFactory,
    IRiderConsistencyLock consistencyLock,
    IRiderLocationPersistence persistence,
    IRiderMatchingTelemetry telemetry,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<UpdateRiderLocationResult> HandleAsync(
        UpdateRiderLocationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Latitude is < -90m or > 90m || command.Longitude is < -180m or > 180m)
        {
            return new(UpdateRiderLocationStatus.InvalidLocation);
        }

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var captured = command.CapturedAtUtc.ToUniversalTime();
        if (captured > now + options.Value.MaximumFutureLocationClockSkew)
        {
            return new(UpdateRiderLocationStatus.InvalidLocation);
        }

        var resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? UpdateRiderLocationStatus.NotAuthenticated
                : UpdateRiderLocationStatus.NotRider);
        }

        try
        {
            return await transactionFactory.ExecuteAsync(
                RiderTransactionIsolation.ReadCommitted,
                new(nameof(UpdateRiderLocationHandler), resolved.Profile!.StakeholderId),
                async (transaction, token) =>
                {
                    await consistencyLock.AcquireRiderAsync(resolved.Profile.StakeholderId, token);
                    var tracked = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, token);
                    if (tracked.Status != RiderActorResolutionStatus.Success)
                    {
                        return new UpdateRiderLocationResult(UpdateRiderLocationStatus.NotRider);
                    }

                    var profile = tracked.Profile!;
                    if (!profile.IsApproved)
                    {
                        return new UpdateRiderLocationResult(UpdateRiderLocationStatus.ProfileInactive);
                    }

                    var location = await persistence.FindAsync(profile.Id, token);
                    var isNew = location is null;
                    location ??= RiderCurrentLocation.Create(profile.Id, captured, now);
                    if (!isNew && !location.TryAdvance(captured, now))
                    {
                        telemetry.RecordStaleLocation();
                        return new UpdateRiderLocationResult(UpdateRiderLocationStatus.StaleLocation);
                    }

                    persistence.TrackPosition(location, command.Latitude, command.Longitude, isNew);
                    await transaction.CommitAsync(token);
                    return new UpdateRiderLocationResult(UpdateRiderLocationStatus.Success);
                },
                cancellationToken);
        }
        catch (RiderTransientPersistenceException)
        {
            return new(UpdateRiderLocationStatus.TemporarilyUnavailable);
        }
    }
}
