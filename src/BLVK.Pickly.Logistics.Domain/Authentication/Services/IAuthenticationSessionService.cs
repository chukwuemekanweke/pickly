using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Services;

public interface IAuthenticationSessionService
{
    Task<AuthenticationSession> CreateAsync(Stakeholder stakeholder, string ipAddress,
        string userAgent, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<AuthenticationSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthenticationSession>> ListActiveAsync(Guid stakeholderId, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(Guid sessionId, Guid stakeholderId, CancellationToken cancellationToken);
    Task RevokeOthersAsync(Guid currentSessionId, Guid stakeholderId, CancellationToken cancellationToken);
    Task RevokeAllAsync(Guid stakeholderId, CancellationToken cancellationToken);
    Task TouchAsync(AuthenticationSession session, string ipAddress, string userAgent, CancellationToken cancellationToken);
}
