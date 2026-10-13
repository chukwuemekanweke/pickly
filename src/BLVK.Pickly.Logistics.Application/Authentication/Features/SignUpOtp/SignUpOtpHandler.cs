using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;

public sealed class SignUpOtpHandler(
    IAuthenticationIdentityService identityService,
    ITwoFactorOtpService twoFactorOtpService,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthenticationSessionService sessionService,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignUpOtpResult> HandleAsync(SignUpOtpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var user = await identityService.FindByEmailAsync(request.Email);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        if (user.EmailConfirmed)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.AlreadyConfirmed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.AlreadyConfirmed));
            return new SignUpOtpResult(SignUpOtpStatus.AlreadyVerified);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            customTelemetryContext.SetProperty(
                Observability.PropertyNames.Common.FailureReason,
                UserSignInFailureReasons.LockedOut);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(
                    request.ActorContext,
                    failureReason: UserSignInFailureReasons.LockedOut));
            return new SignUpOtpResult(SignUpOtpStatus.AccountUnavailable);
        }

        if (!await twoFactorOtpService.ValidateOtpAsync(
                user.Id,
                request.Otp,
                OtpIntent.EmailConfirmation,
                cancellationToken))
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidOtp);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.EmailConfirmationFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidOtp));
            return new SignUpOtpResult(SignUpOtpStatus.InvalidOtp);
        }

        var now = timeProvider.GetUtcNow();
        user.MarkEmailVerified();
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var updateResult = await identityService.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            if (updateResult.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
            {
                return new SignUpOtpResult(SignUpOtpStatus.AlreadyVerified);
            }

            throw new InvalidOperationException("Failed to update the user after OTP verification.");
        }

        var session = await sessionService.CreateAsync(stakeholder, request.IpAddress,
            request.UserAgent, refreshTokenService.GetExpiry(AuthenticationOtpDefaults.EmailConfirmationSessionLifetime), cancellationToken);
        var accessToken = accessTokenService.Generate(user, stakeholder.Id, session.Id);
        var refreshToken = await refreshTokenService.IssueAsync(
            user,
            session.Id,
            session.ExpiresAtUtc,
            cancellationToken);

        await eventPublisher.PublishAsync(new UserEmailConfirmed
        {
            StakeholderId = stakeholder.Id,
            FlowId = request.ActorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);
        await eventPublisher.PublishAsync(new UserSignInSuccessful(request.IpAddress, request.UserAgent)
        {
            StakeholderId = stakeholder.Id,
            FlowId = request.ActorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new SignUpOtpResult(
            SignUpOtpStatus.Success,
            new AuthenticationTokens(accessToken, refreshToken));
    }
}
