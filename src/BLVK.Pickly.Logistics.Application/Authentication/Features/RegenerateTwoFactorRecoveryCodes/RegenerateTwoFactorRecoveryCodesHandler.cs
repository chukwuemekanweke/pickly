using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public sealed class RegenerateTwoFactorRecoveryCodesHandler(
    TwoFactorActorResolver actorResolver,
    TwoFactorProofVerifier proofVerifier,
    IAuthenticationIdentityService identityService,
    IAuthenticationSessionService sessionService,
    IOptions<TwoFactorAuthenticationOptions> options,
    ICustomTelemetryContext telemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<RegenerateTwoFactorRecoveryCodesResult> HandleAsync(
        RegenerateTwoFactorRecoveryCodesCommand command,
        CancellationToken cancellationToken)
    {
        var actor = await actorResolver.ResolveAsync(command.ActorContext, cancellationToken);
        if (actor is null)
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.NotAuthenticated);
        if (!await identityService.GetTwoFactorEnabledAsync(actor.User))
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.NotEnabled);
        if (!await proofVerifier.VerifyAsync(actor.User, command.VerificationMethod, command.Code))
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.InvalidProof);

        var codes = (await identityService.GenerateNewTwoFactorRecoveryCodesAsync(
            actor.User,
            options.Value.RecoveryCodeCount))?.ToArray();
        if (codes is null || codes.Length == 0)
            return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.Failed);

        await sessionService.RevokeOthersAsync(command.CurrentSessionId, actor.StakeholderId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        telemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.TwoFactorRecoveryCodesRegenerated,
            ObservabilityEventProperties.Create(command.ActorContext, actor.StakeholderId));
        return new RegenerateTwoFactorRecoveryCodesResult(RegenerateTwoFactorRecoveryCodesStatus.Success, codes);
    }
}
