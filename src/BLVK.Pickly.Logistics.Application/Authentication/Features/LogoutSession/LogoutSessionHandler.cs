using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.LogoutSession;

public sealed class LogoutSessionHandler(
    IAccessTokenRevocationService accessTokenRevocationService,
    ICustomTelemetryContext customTelemetryContext,
    IAuthenticationSessionService sessionService,
    IUnitOfWork unitOfWork)
{
    public async Task<LogoutSessionResult> HandleAsync(LogoutSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenId) || request.SessionId == Guid.Empty ||
            request.StakeholderId == Guid.Empty)
        {
            return new LogoutSessionResult(LogoutSessionStatus.InvalidToken);
        }

        if (!await sessionService.RevokeAsync(request.SessionId, request.StakeholderId, cancellationToken))
        {
            return new LogoutSessionResult(LogoutSessionStatus.InvalidToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await accessTokenRevocationService.RevokeAsync(request.TokenId, request.ExpiresAtUtc, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SignOutCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, request.StakeholderId));

        return new LogoutSessionResult(LogoutSessionStatus.Success);
    }
}
