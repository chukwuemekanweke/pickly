using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GetTwoFactorStatus;

public sealed class GetTwoFactorStatusHandler(
    TwoFactorActorResolver actorResolver,
    IAuthenticationIdentityService identityService)
{
    public async Task<GetTwoFactorStatusResult> HandleAsync(
        GetTwoFactorStatusQuery query,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(query.ActorContext, cancellationToken);
        if (actor is null)
            return new GetTwoFactorStatusResult(GetTwoFactorStatusStatus.NotAuthenticated);

        var enabled = await identityService.GetTwoFactorEnabledAsync(actor.User);
        var count = enabled ? await identityService.CountRecoveryCodesAsync(actor.User) : 0;
        return new GetTwoFactorStatusResult(GetTwoFactorStatusStatus.Success, enabled, count);
    }
}
