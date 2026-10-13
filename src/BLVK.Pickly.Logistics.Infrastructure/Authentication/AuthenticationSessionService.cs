using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Authentication.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class AuthenticationSessionService(
    IRepository<AuthenticationSession> repository,
    IIpAddressResolver ipAddressResolver,
    IUserAgentParserService userAgentParser,
    TimeProvider timeProvider) : IAuthenticationSessionService
{
    public async Task<AuthenticationSession> CreateAsync(Stakeholder stakeholder,
        string ipAddress, string userAgent, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var ip = await ipAddressResolver.ResolveAsync(ipAddress, cancellationToken);
        var device = userAgentParser.Parse(userAgent);
        var session = AuthenticationSession.Create(stakeholder.Id,
            ip.IpAddressId, userAgent, device.DeviceName, device.DevicePlatform, device.BrowserName,
            timeProvider.GetUtcNow(), expiresAtUtc);
        await repository.AddAsync(session, cancellationToken);
        return session;
    }

    public Task<AuthenticationSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(sessionId, cancellationToken);

    public Task<IReadOnlyList<AuthenticationSession>> ListActiveAsync(Guid stakeholderId, CancellationToken cancellationToken) =>
        repository.ListAsync(new ActiveSessionsForStakeholderSpecification(stakeholderId, timeProvider.GetUtcNow()), cancellationToken);

    public async Task<bool> RevokeAsync(Guid sessionId, Guid stakeholderId, CancellationToken cancellationToken)
    {
        var session = await FindAsync(sessionId, cancellationToken);
        if (session is null || session.StakeholderId != stakeholderId) return false;
        session.Revoke(timeProvider.GetUtcNow());
        repository.Update(session);
        return true;
    }

    public async Task RevokeOthersAsync(Guid currentSessionId, Guid stakeholderId, CancellationToken cancellationToken)
    {
        foreach (var session in await ListActiveAsync(stakeholderId, cancellationToken))
        {
            if (session.Id != currentSessionId)
            {
                session.Revoke(timeProvider.GetUtcNow());
                repository.Update(session);
            }
        }
    }

    public async Task RevokeAllAsync(Guid stakeholderId, CancellationToken cancellationToken)
    {
        foreach (var session in await ListActiveAsync(stakeholderId, cancellationToken))
        {
            session.Revoke(timeProvider.GetUtcNow());
            repository.Update(session);
        }
    }

    public async Task TouchAsync(AuthenticationSession session, string ipAddress, string userAgent,
        CancellationToken cancellationToken)
    {
        var ip = await ipAddressResolver.ResolveAsync(ipAddress, cancellationToken);
        var device = userAgentParser.Parse(userAgent);
        session.Touch(timeProvider.GetUtcNow(), ip.IpAddressId, userAgent,
            device.DeviceName, device.DevicePlatform, device.BrowserName);
        repository.Update(session);
    }
}
