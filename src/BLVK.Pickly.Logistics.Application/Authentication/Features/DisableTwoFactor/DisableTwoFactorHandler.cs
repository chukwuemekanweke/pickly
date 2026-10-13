using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.DisableTwoFactor;

public sealed class DisableTwoFactorHandler(
    TwoFactorActorResolver actorResolver,
    TwoFactorProofVerifier proofVerifier,
    IAuthenticationIdentityService identityService,
    IAuthenticationSessionService sessionService,
    ICustomTelemetryContext telemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<DisableTwoFactorResult> HandleAsync(
        DisableTwoFactorCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return DisableTwoFactorResult.NotAuthenticated;
        if (!await identityService.GetTwoFactorEnabledAsync(actor.User))
            return DisableTwoFactorResult.NotEnabled;
        if (!await proofVerifier.VerifyAsync(actor.User, command.VerificationMethod, command.Code))
            return DisableTwoFactorResult.InvalidProof;

        if (!(await identityService.SetTwoFactorEnabledAsync(actor.User, false)).Succeeded)
            return DisableTwoFactorResult.Failed;
        if (!(await identityService.ResetAuthenticatorKeyAsync(actor.User)).Succeeded)
            return DisableTwoFactorResult.Failed;
        await sessionService.RevokeOthersAsync(command.CurrentSessionId, actor.StakeholderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.TwoFactorDisabled,
            ObservabilityEventProperties.Create(command.ActorContext, actor.StakeholderId));
        return DisableTwoFactorResult.Success;
    }
}
