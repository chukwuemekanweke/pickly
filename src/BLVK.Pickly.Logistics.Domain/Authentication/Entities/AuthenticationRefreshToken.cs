using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Entities;

public sealed class AuthenticationRefreshToken : Entity, IAggregateRoot
{
    private AuthenticationRefreshToken()
    {
    }

    private AuthenticationRefreshToken(
        Guid appUserId,
        Guid authenticationSessionId,
        string tokenHash,
        string securityStamp,
        DateTimeOffset expiresAtUtc)
    {
        AppUserId = appUserId;
        AuthenticationSessionId = authenticationSessionId;
        TokenHash = tokenHash;
        SecurityStamp = securityStamp;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AppUserId { get; private set; }
    public Guid? AuthenticationSessionId { get; private set; }
    public AuthenticationSession? AuthenticationSession { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string SecurityStamp { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public AppUser AppUser { get; private set; } = null!;

    public static AuthenticationRefreshToken Create(
        Guid appUserId,
        Guid authenticationSessionId,
        string tokenHash,
        string securityStamp,
        DateTimeOffset expiresAtUtc) =>
        new(appUserId, authenticationSessionId, tokenHash, securityStamp, expiresAtUtc);

    public bool CanBeRedeemed(string securityStamp, DateTimeOffset utcNow) =>
        RevokedAtUtc is null
        && ExpiresAtUtc > utcNow
        && string.Equals(SecurityStamp, securityStamp ?? string.Empty, StringComparison.Ordinal);

    public void Revoke(DateTimeOffset utcNow)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = utcNow;
    }
}
