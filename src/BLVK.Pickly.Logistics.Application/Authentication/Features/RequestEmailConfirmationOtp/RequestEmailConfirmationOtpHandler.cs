using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;

public sealed class RequestEmailConfirmationOtpHandler(
    IAuthenticationIdentityService identityService,
    ITwoFactorOtpService twoFactorOtpService,
    ICommandSender commandSender,
    StakeholderResolver stakeholderResolver,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RequestEmailConfirmationOtpResult> HandleAsync(
        RequestEmailConfirmationOtpCommand request,
        CancellationToken cancellationToken)
    {
        var requestedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = requestedAtUtc.Add(AuthenticationOtpDefaults.EmailConfirmationLifetime);
        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to request an email confirmation OTP.");
        var user = await identityService.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return new RequestEmailConfirmationOtpResult(RequestEmailConfirmationOtpStatus.UserNotFound, expiresAtUtc);
        }

        if (user.EmailConfirmed)
        {
            return new RequestEmailConfirmationOtpResult(RequestEmailConfirmationOtpStatus.AlreadyVerified, expiresAtUtc);
        }

        var activeOtp = await twoFactorOtpService.GetActiveOtpAsync(
            user.Id,
            OtpIntent.EmailConfirmation,
            cancellationToken);
        if (activeOtp is not null)
        {
            return new RequestEmailConfirmationOtpResult(
                RequestEmailConfirmationOtpStatus.Accepted,
                activeOtp.ExpiresAtUtc);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        await commandSender.SendAsync(
            new SendEmailConfirmationOtpCommand
            {
                StakeholderId = stakeholder.Id,
                TenantId = tenantId,
                FlowId = request.ActorContext.FlowId,
                RequestedAt = requestedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestEmailConfirmationOtpResult(RequestEmailConfirmationOtpStatus.Accepted, expiresAtUtc);
    }
}
