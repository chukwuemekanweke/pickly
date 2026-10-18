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

        await using var transaction = await transactionFactory.BeginAsync(
            RiderTransactionIsolation.ReadCommitted,
            cancellationToken);
        await consistencyLock.AcquireRiderAsync(resolved.Profile!.StakeholderId, cancellationToken);
        resolved = await actorResolver.ResolveAsync(command.ActorContext, trackProfile: true, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(UpdateRiderLocationStatus.NotRider);
        }

        var profile = resolved.Profile!;
        if (!profile.IsApproved)
        {
            return new(UpdateRiderLocationStatus.ProfileInactive);
        }

        var location = await persistence.FindAsync(profile.Id, cancellationToken);
        var isNew = location is null;
        location ??= RiderCurrentLocation.Create(profile.Id, captured, now);
        if (!isNew && !location.TryAdvance(captured, now))
        {
            telemetry.RecordStaleLocation();
            return new(UpdateRiderLocationStatus.StaleLocation);
        }

        persistence.TrackPosition(location, command.Latitude, command.Longitude, isNew);
        await transaction.CommitAsync(cancellationToken);
        return new(UpdateRiderLocationStatus.Success);
    }
}
