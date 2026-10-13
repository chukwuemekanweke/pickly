using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;

public sealed class SignInHandler(
    IAuthenticationIdentityService identityService,
    PasswordCredentialVerifier passwordCredentialVerifier,
    AuthenticationSessionIssuer sessionIssuer,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ITwoFactorChallengeService twoFactorChallengeService)
{
    public async Task<SignInResult> HandleAsync(SignInCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignInStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var user = await identityService.FindByEmailAsync(request.Email);

        if (user is null)
        {
            await PublishFailedAsync(
                stakeholderId: null,
                emailAddress: request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.UserNotFound,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.LockedOut,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason: UserSignInFailureReasons.EmailNotVerified,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SignInResult(SignInStatus.EmailNotVerified, null);
        }

        var passwordStatus = await passwordCredentialVerifier.VerifyAsync(user, request.Password);
        if (passwordStatus != PasswordCredentialVerificationStatus.Success)
        {
            var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
            var failureReason = passwordStatus == PasswordCredentialVerificationStatus.Locked
                ? UserSignInFailureReasons.LockedOut
                : UserSignInFailureReasons.InvalidCredentials;
            await PublishFailedAsync(
                stakeholderId: stakeholder.Id,
                emailAddress: user.Email ?? request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                failureReason,
                request.ActorContext,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return passwordStatus == PasswordCredentialVerificationStatus.Locked
                ? new SignInResult(SignInStatus.AccountLocked, null, await identityService.GetLockoutEndUtcAsync(user))
                : new SignInResult(SignInStatus.InvalidCredentials, null);
        }

        var currentStakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (await identityService.GetTwoFactorEnabledAsync(user))
        {
            var challenge = await twoFactorChallengeService.StartAsync(
                user.Id,
                currentStakeholder.Id,
                currentStakeholder.TenantId,
                AuthenticationMethod.Password,
                request.ActorContext,
                request.IpAddress,
                request.UserAgent,
                cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.TwoFactorChallengeRequired,
                ObservabilityEventProperties.Create(request.ActorContext, currentStakeholder.Id));
            return new SignInResult(SignInStatus.RequiresTwoFactor, null, Challenge: challenge);
        }

        var tokens = await sessionIssuer.IssueAsync(
            user,
            currentStakeholder,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);

        await PublishSuccessfulAsync(
            stakeholderId: currentStakeholder.Id,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            request.ActorContext,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SignInResult(SignInStatus.Success, tokens);
    }

    private async Task PublishSuccessfulAsync(
        Guid stakeholderId,
        string ipAddress,
        string userAgent,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInSuccessful(ipAddress, userAgent)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignInCompleted,
            ObservabilityEventProperties.Create(actorContext, stakeholderId));
    }

    private async Task PublishFailedAsync(
        Guid? stakeholderId,
        string emailAddress,
        string ipAddress,
        string userAgent,
        string failureReason,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserSignInFailed(emailAddress, ipAddress, userAgent, failureReason)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        if (stakeholderId.HasValue)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
        }

        customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, failureReason);
    }
}
