using BLVK.Pickly.Logistics.Application.Authentication.Stakeholders;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;

public sealed class RefreshSessionHandler(
    IAuthenticationIdentityService identityService,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthenticationSessionService sessionService,
    IEventPublisher eventPublisher,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RefreshSessionResult> HandleAsync(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var currentRefreshToken = await refreshTokenService.FindByTokenAsync(request.RefreshToken, cancellationToken);
        if (currentRefreshToken is null)
        {
            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        var session = currentRefreshToken.AuthenticationSessionId.HasValue
            ? await sessionService.FindAsync(currentRefreshToken.AuthenticationSessionId.Value, cancellationToken)
            : null;
        if (session is null || !session.IsActive(timeProvider.GetUtcNow()))
        {
            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        var user = await identityService.FindByIdAsync(currentRefreshToken.AppUserId);
        if (user is null)
        {
            refreshTokenService.Revoke(currentRefreshToken, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        var currentStakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (session.StakeholderId != currentStakeholder.Id)
        {
            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        var currentSecurityStamp = await identityService.GetSecurityStampAsync(user);
        var utcNow = timeProvider.GetUtcNow();
        if (!currentRefreshToken.CanBeRedeemed(currentSecurityStamp, utcNow))
        {
            refreshTokenService.Revoke(currentRefreshToken, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshSessionResult(RefreshSessionStatus.InvalidRefreshToken, null);
        }

        if (await identityService.IsLockedOutAsync(user))
        {
            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user);
            return new RefreshSessionResult(RefreshSessionStatus.AccountLocked, null, lockedUntilUtc);
        }

        if (!user.EmailConfirmed)
        {
            return new RefreshSessionResult(RefreshSessionStatus.EmailNotVerified, null);
        }

        var accessToken = accessTokenService.Generate(user, currentStakeholder.Id, session.Id);
        var refreshToken = await refreshTokenService.RotateAsync(currentRefreshToken, user, cancellationToken);
        await sessionService.TouchAsync(session, request.IpAddress, request.UserAgent, cancellationToken);

        await PublishTokenRefreshedAsync(
            stakeholderId: currentStakeholder.Id,
            ipAddress: request.IpAddress,
            userAgent: request.UserAgent,
            request.ActorContext,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshSessionResult(
            RefreshSessionStatus.Success,
            new AuthenticationTokens(accessToken, refreshToken));
    }

    private async Task PublishTokenRefreshedAsync(
        Guid stakeholderId,
        string ipAddress,
        string userAgent,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await eventPublisher.PublishAsync(new UserAccessTokenRefreshed(ipAddress, userAgent)
        {
            StakeholderId = stakeholderId,
            FlowId = actorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SessionRefreshCompleted,
            ObservabilityEventProperties.Create(actorContext, stakeholderId));
    }
}
