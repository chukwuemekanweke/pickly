using BLVK.Pickly.Logistics.Domain.Riders;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderStatus;

public sealed class GetRiderStatusHandler(
    RiderActorResolver actorResolver,
    RiderStatusProjector statusProjector,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<GetRiderStatusResult> HandleAsync(
        GetRiderStatusQuery query,
        CancellationToken cancellationToken)
    {
        var resolved = await actorResolver.ResolveAsync(query.ActorContext, trackProfile: false, cancellationToken);
        if (resolved.Status != RiderActorResolutionStatus.Success)
        {
            return new(resolved.Status == RiderActorResolutionStatus.NotAuthenticated
                ? GetRiderStatusStatus.NotAuthenticated
                : GetRiderStatusStatus.NotRider);
        }

        var status = await statusProjector.ProjectAsync(
            resolved.Profile!,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);
        return new(GetRiderStatusStatus.Success, status);
    }
}
