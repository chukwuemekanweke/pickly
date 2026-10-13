using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RequestPasswordReset;

public sealed class RequestPasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ICommandSender commandSender,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<RequestPasswordResetResult> HandleAsync(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to request a password reset.");
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await identityService.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.UserNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasswordResetRequestFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.UserNotFound));
            return new RequestPasswordResetResult(RequestPasswordResetStatus.UserNotFound);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

        await commandSender.SendAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholder.Id,
                TenantId = tenantId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetRequested,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new RequestPasswordResetResult(RequestPasswordResetStatus.Success);
    }
}
