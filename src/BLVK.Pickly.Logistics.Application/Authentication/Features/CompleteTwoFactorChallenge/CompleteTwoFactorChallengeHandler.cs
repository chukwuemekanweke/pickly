using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;

public sealed class CompleteTwoFactorChallengeHandler(
    ITwoFactorChallengeService challengeService,
    IAuthenticationIdentityService identityService,
    AuthenticationSessionIssuer sessionIssuer,
    StakeholderResolver stakeholderResolver,
    IEventPublisher eventPublisher,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CompleteTwoFactorChallengeResult> HandleAsync(
        CompleteTwoFactorChallengeCommand command,
        CancellationToken cancellationToken)
    {
        var taken = await challengeService.TakeAsync(command.Challenge, cancellationToken);
        if (taken.Status != TwoFactorChallengeStatus.Success)
            return new CompleteTwoFactorChallengeResult(MapStatus(taken.Status));

        var challenge = taken.Challenge!;
        var user = await identityService.FindByIdAsync(challenge.AppUserId);
        if (user is null || !await identityService.GetTwoFactorEnabledAsync(user))
            return new CompleteTwoFactorChallengeResult(CompleteTwoFactorChallengeStatus.InvalidChallenge);

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (stakeholder.Id != challenge.StakeholderId || stakeholder.TenantId != challenge.TenantId)
            return new CompleteTwoFactorChallengeResult(CompleteTwoFactorChallengeStatus.InvalidChallenge);

        if (await identityService.IsLockedOutAsync(user))
            return new CompleteTwoFactorChallengeResult(
                CompleteTwoFactorChallengeStatus.AccountLocked,
                LockedUntilUtc: await identityService.GetLockoutEndUtcAsync(user));

        if (!user.EmailConfirmed)
            return new CompleteTwoFactorChallengeResult(CompleteTwoFactorChallengeStatus.EmailNotVerified);

        var verified = command.VerificationMethod switch
        {
            TwoFactorVerificationMethod.Authenticator =>
                await identityService.VerifyAuthenticatorTokenAsync(user, command.Code),
            TwoFactorVerificationMethod.RecoveryCode =>
                (await identityService.RedeemTwoFactorRecoveryCodeAsync(user, command.Code)).Succeeded,
            _ => false
        };

        if (!verified)
        {
            await identityService.AccessFailedAsync(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var next = challenge with { RemainingAttempts = challenge.RemainingAttempts - 1 };
            var locked = await identityService.IsLockedOutAsync(user);
            if (!locked)
                await challengeService.RestoreAsync(next, cancellationToken);

            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.TwoFactorVerificationFailed,
                ObservabilityEventProperties.Create(challenge.ActorContext, stakeholder.Id));

            if (locked)
                return new CompleteTwoFactorChallengeResult(
                    CompleteTwoFactorChallengeStatus.AccountLocked,
                    LockedUntilUtc: await identityService.GetLockoutEndUtcAsync(user));

            return new CompleteTwoFactorChallengeResult(next.RemainingAttempts > 0
                ? CompleteTwoFactorChallengeStatus.InvalidCode
                : CompleteTwoFactorChallengeStatus.ExhaustedChallenge);
        }

        await identityService.ResetAccessFailedCountAsync(user);
        var tokens = await sessionIssuer.IssueAsync(
            user,
            stakeholder,
            challenge.IpAddress,
            challenge.UserAgent,
            cancellationToken);
        await eventPublisher.PublishAsync(new UserSignInSuccessful(challenge.IpAddress, challenge.UserAgent)
        {
            StakeholderId = stakeholder.Id,
            FlowId = challenge.ActorContext.FlowId,
            OccuredAt = timeProvider.GetUtcNow()
        }, cancellationToken);
        customTelemetryContext.AddCustomEvent(
            challenge.AuthenticationMethod == AuthenticationMethod.Password
                ? Observability.EventNames.Authentication.PasswordSignInCompleted
                : Observability.EventNames.Authentication.GoogleSignInCompleted,
            ObservabilityEventProperties.Create(challenge.ActorContext, stakeholder.Id));
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.TwoFactorVerificationCompleted,
            ObservabilityEventProperties.Create(challenge.ActorContext, stakeholder.Id));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteTwoFactorChallengeResult(CompleteTwoFactorChallengeStatus.Success, tokens);
    }

    private static CompleteTwoFactorChallengeStatus MapStatus(TwoFactorChallengeStatus status) => status switch
    {
        TwoFactorChallengeStatus.Expired => CompleteTwoFactorChallengeStatus.ExpiredChallenge,
        TwoFactorChallengeStatus.Consumed => CompleteTwoFactorChallengeStatus.ConsumedChallenge,
        TwoFactorChallengeStatus.Exhausted => CompleteTwoFactorChallengeStatus.ExhaustedChallenge,
        _ => CompleteTwoFactorChallengeStatus.InvalidChallenge
    };
}
