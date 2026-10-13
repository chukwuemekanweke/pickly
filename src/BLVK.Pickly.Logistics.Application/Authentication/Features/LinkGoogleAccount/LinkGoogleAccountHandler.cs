using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;

public sealed class LinkGoogleAccountHandler(
    IAuthenticationIdentityService identityService,
    IGoogleAuthenticationFlowService googleAuthenticationFlowService,
    PasswordCredentialVerifier passwordCredentialVerifier,
    AuthenticationSessionIssuer sessionIssuer,
    StakeholderResolver stakeholderResolver,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<LinkGoogleAccountResult> HandleAsync(
        LinkGoogleAccountCommand request,
        CancellationToken cancellationToken)
    {
        var flowResult = await googleAuthenticationFlowService.TakeAsync(request.FlowToken, cancellationToken);
        if (flowResult.Status != GoogleAuthenticationFlowStatus.Success)
        {
            return new LinkGoogleAccountResult(MapFlowStatus(flowResult.Status));
        }

        var flow = flowResult.Flow!;
        if (flow.State != GoogleAuthenticationFlowState.LinkRequired
            || (flow.TenantId != Guid.Empty && request.ActorContext.TenantId != flow.TenantId)
            || string.IsNullOrWhiteSpace(flow.Subject)
            || string.IsNullOrWhiteSpace(flow.Email))
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new LinkGoogleAccountResult(LinkGoogleAccountStatus.GoogleFlowInvalid);
        }

        var user = await identityService.FindByEmailAsync(flow.Email);
        if (user is null)
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new LinkGoogleAccountResult(LinkGoogleAccountStatus.GoogleFlowInvalid);
        }

        var passwordStatus = await passwordCredentialVerifier.VerifyAsync(user, request.Password);
        if (passwordStatus != PasswordCredentialVerificationStatus.Success)
        {
            await googleAuthenticationFlowService.RestoreAsync(flow, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return passwordStatus == PasswordCredentialVerificationStatus.Locked
                ? new LinkGoogleAccountResult(
                    LinkGoogleAccountStatus.AccountLocked,
                    LockedUntilUtc: await identityService.GetLockoutEndUtcAsync(user))
                : new LinkGoogleAccountResult(LinkGoogleAccountStatus.InvalidCredentials);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (flow.TenantId != Guid.Empty && stakeholder.TenantId != flow.TenantId)
        {
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new LinkGoogleAccountResult(LinkGoogleAccountStatus.GoogleFlowInvalid);
        }

        var loginResult = await identityService.AddLoginAsync(
            user,
            ExternalLoginProviders.Google,
            flow.Subject,
            ExternalLoginProviders.Google);
        if (!loginResult.Succeeded)
        {
            var associatedUser = await identityService.FindByLoginAsync(ExternalLoginProviders.Google, flow.Subject);
            if (associatedUser?.Id != user.Id
                || loginResult.Errors.Any(error => error.Code != nameof(IdentityErrorDescriber.LoginAlreadyAssociated)))
            {
                await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
                return new LinkGoogleAccountResult(LinkGoogleAccountStatus.GoogleAccountAlreadyLinked);
            }
        }

        if (!user.EmailConfirmed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);
            return new LinkGoogleAccountResult(LinkGoogleAccountStatus.EmailVerificationRequired);
        }

        var tokens = await sessionIssuer.IssueAsync(
            user,
            stakeholder,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);
        await eventPublisher.PublishAsync(new UserSignInSuccessful(request.IpAddress, request.UserAgent)
        {
            StakeholderId = stakeholder.Id,
            FlowId = request.ActorContext.FlowId,
            OccuredAt = timeProvider.GetUtcNow()
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await googleAuthenticationFlowService.ConsumeAsync(flow, cancellationToken);

        return new LinkGoogleAccountResult(LinkGoogleAccountStatus.Success, tokens);
    }

    private static LinkGoogleAccountStatus MapFlowStatus(GoogleAuthenticationFlowStatus status) => status switch
    {
        GoogleAuthenticationFlowStatus.Expired => LinkGoogleAccountStatus.GoogleFlowExpired,
        GoogleAuthenticationFlowStatus.Consumed => LinkGoogleAccountStatus.GoogleFlowConsumed,
        _ => LinkGoogleAccountStatus.GoogleFlowInvalid
    };
}
