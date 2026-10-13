using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class GoogleAuthenticationFlowService(
    IJsonCache cache,
    IOptions<GoogleAuthenticationOptions> options,
    TimeProvider timeProvider) : IGoogleAuthenticationFlowService
{
    private static readonly TimeSpan MarkerRetention = TimeSpan.FromMinutes(5);

    public async Task<GoogleAuthenticationFlow> StartAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var flow = new GoogleAuthenticationFlow(
            CreateOpaqueValue(),
            CreateOpaqueValue(),
            tenantId,
            GoogleAuthenticationFlowState.Initiated,
            now.Add(options.Value.FlowLifetime));

        await RestoreAsync(flow, cancellationToken);
        return flow;
    }

    public async Task<GoogleAuthenticationFlowResult> TakeAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new GoogleAuthenticationFlowResult(GoogleAuthenticationFlowStatus.Invalid);
        }

        var flow = await cache.GetAndRemoveAsync<GoogleAuthenticationFlow>(DataKey(token), cancellationToken);
        if (flow is not null)
        {
            if (flow.ExpiresAtUtc <= timeProvider.GetUtcNow())
            {
                await SetMarkerAsync(flow, consumed: false, cancellationToken);
                return new GoogleAuthenticationFlowResult(GoogleAuthenticationFlowStatus.Expired);
            }

            await SetMarkerAsync(flow, consumed: true, cancellationToken);
            return new GoogleAuthenticationFlowResult(GoogleAuthenticationFlowStatus.Success, flow);
        }

        var marker = await cache.GetAsync<FlowMarker>(MarkerKey(token), cancellationToken);
        if (marker is null)
        {
            return new GoogleAuthenticationFlowResult(GoogleAuthenticationFlowStatus.Invalid);
        }

        return new GoogleAuthenticationFlowResult(
            marker.Consumed
                ? GoogleAuthenticationFlowStatus.Consumed
                : GoogleAuthenticationFlowStatus.Expired);
    }

    public async Task RestoreAsync(GoogleAuthenticationFlow flow, CancellationToken cancellationToken)
    {
        var remaining = flow.ExpiresAtUtc - timeProvider.GetUtcNow();
        if (remaining <= TimeSpan.Zero)
        {
            await SetMarkerAsync(flow, consumed: false, cancellationToken);
            return;
        }

        await cache.SetAsync(DataKey(flow.Token), flow, remaining, cancellationToken);
        await SetMarkerAsync(flow, consumed: false, cancellationToken);
    }

    public async Task ConsumeAsync(GoogleAuthenticationFlow flow, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(DataKey(flow.Token), cancellationToken);
        await SetMarkerAsync(flow, consumed: true, cancellationToken);
    }

    private Task SetMarkerAsync(GoogleAuthenticationFlow flow, bool consumed, CancellationToken cancellationToken) =>
        cache.SetAsync(
            MarkerKey(flow.Token),
            new FlowMarker(flow.ExpiresAtUtc, consumed),
            Max(flow.ExpiresAtUtc - timeProvider.GetUtcNow() + MarkerRetention, MarkerRetention),
            cancellationToken);

    private static string CreateOpaqueValue() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string DataKey(string token) => $"authentication:google-flow:{token}";
    private static string MarkerKey(string token) => $"authentication:google-flow-marker:{token}";
    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;

    private sealed record FlowMarker(DateTimeOffset ExpiresAtUtc, bool Consumed);
}
