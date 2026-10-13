using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class TwoFactorChallengeService(
    IJsonCache cache,
    IOptions<TwoFactorAuthenticationOptions> options,
    TimeProvider timeProvider) : ITwoFactorChallengeService
{
    private static readonly TimeSpan MarkerRetention = TimeSpan.FromMinutes(5);

    public async Task<TwoFactorChallenge> StartAsync(
        Guid appUserId,
        Guid stakeholderId,
        Guid tenantId,
        AuthenticationMethod authenticationMethod,
        ActorContext actorContext,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var challenge = new TwoFactorChallenge(
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
            appUserId,
            stakeholderId,
            tenantId,
            authenticationMethod,
            actorContext,
            ipAddress,
            userAgent,
            now.Add(options.Value.ChallengeLifetime),
            options.Value.ChallengeAttempts);

        await RestoreAsync(challenge, cancellationToken);
        return challenge;
    }

    public async Task<TwoFactorChallengeResult> TakeAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new TwoFactorChallengeResult(TwoFactorChallengeStatus.Invalid);

        var challenge = await cache.GetAndRemoveAsync<TwoFactorChallenge>(DataKey(token), cancellationToken);
        if (challenge is not null)
        {
            if (challenge.ExpiresAtUtc <= timeProvider.GetUtcNow())
            {
                await SetMarkerAsync(challenge, TwoFactorChallengeStatus.Expired, cancellationToken);
                return new TwoFactorChallengeResult(TwoFactorChallengeStatus.Expired);
            }

            await SetMarkerAsync(challenge, TwoFactorChallengeStatus.Consumed, cancellationToken);
            return new TwoFactorChallengeResult(TwoFactorChallengeStatus.Success, challenge);
        }

        var marker = await cache.GetAsync<ChallengeMarker>(MarkerKey(token), cancellationToken);
        return marker is null
            ? new TwoFactorChallengeResult(TwoFactorChallengeStatus.Invalid)
            : new TwoFactorChallengeResult(marker.Status);
    }

    public async Task RestoreAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken)
    {
        var remaining = challenge.ExpiresAtUtc - timeProvider.GetUtcNow();
        if (remaining <= TimeSpan.Zero)
        {
            await SetMarkerAsync(challenge, TwoFactorChallengeStatus.Expired, cancellationToken);
            return;
        }

        if (challenge.RemainingAttempts <= 0)
        {
            await SetMarkerAsync(challenge, TwoFactorChallengeStatus.Exhausted, cancellationToken);
            return;
        }

        await cache.SetAsync(DataKey(challenge.Token), challenge, remaining, cancellationToken);
        await SetMarkerAsync(challenge, TwoFactorChallengeStatus.Invalid, cancellationToken);
    }

    private Task SetMarkerAsync(
        TwoFactorChallenge challenge,
        TwoFactorChallengeStatus status,
        CancellationToken cancellationToken) =>
        cache.SetAsync(
            MarkerKey(challenge.Token),
            new ChallengeMarker(status),
            Max(challenge.ExpiresAtUtc - timeProvider.GetUtcNow() + MarkerRetention, MarkerRetention),
            cancellationToken);

    private static string DataKey(string token) => $"authentication:two-factor-challenge:{token}";
    private static string MarkerKey(string token) => $"authentication:two-factor-challenge-marker:{token}";
    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;

    private sealed record ChallengeMarker(TwoFactorChallengeStatus Status);
}
