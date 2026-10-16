using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal sealed class MapboxDeliveryRouteProvider(HttpClient httpClient, IJsonCache cache, IOptions<DeliveryRoutingOptions> options, TimeProvider timeProvider, ILogger<MapboxDeliveryRouteProvider> logger) : IDeliveryRouteProvider
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Profiles = ["driving", "driving-traffic", "walking", "cycling"];
    public async Task<DeliveryRouteResult> GetRouteAsync(DeliveryRouteRequest request, CancellationToken cancellationToken)
    {
        var profile = request.RoutingProfile?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!Profiles.Contains(profile)) return new DeliveryRouteResult(DeliveryRouteStatus.UnsupportedProfile, RoutingProfile: profile);
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AccessToken)) return new DeliveryRouteResult(DeliveryRouteStatus.MissingConfiguration, RoutingProfile: profile);
        var key = $"delivery-route:v1:mapbox:{request.OriginLocationId:N}:{request.DestinationLocationId:N}:{profile}";
        var cached = await cache.GetAsync<CachedRoute>(key, cancellationToken);
        if (cached is not null) return cached.ToResult(profile);
        var gate = Gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cached = await cache.GetAsync<CachedRoute>(key, cancellationToken);
            if (cached is not null) return cached.ToResult(profile);
            var result = await SendAsync(request, profile, settings, cancellationToken);
            if (result.Status == DeliveryRouteStatus.Success)
                await cache.SetAsync(key, new CachedRoute(result.DistanceMeters, result.Duration?.TotalSeconds), settings.CacheDuration, cancellationToken);
            return result;
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1) Gates.TryRemove(key, out _);
        }
    }

    private async Task<DeliveryRouteResult> SendAsync(DeliveryRouteRequest request, string profile, DeliveryRoutingOptions settings, CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, BuildUri(request, profile, settings.AccessToken));
            using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) return new DeliveryRouteResult(DeliveryRouteStatus.RateLimited, Provider: "mapbox", RoutingProfile: profile, RetryAfter: ResolveRetryAfter(response, settings));
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return new DeliveryRouteResult(DeliveryRouteStatus.ProviderUnavailable, Provider: "mapbox", RoutingProfile: profile);
            if ((int)response.StatusCode >= 500) return new DeliveryRouteResult(DeliveryRouteStatus.ProviderUnavailable, Provider: "mapbox", RoutingProfile: profile);
            if (response.StatusCode != HttpStatusCode.OK) return new DeliveryRouteResult(DeliveryRouteStatus.InvalidResponse, Provider: "mapbox", RoutingProfile: profile);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var code = root.TryGetProperty("code", out var codeNode) ? codeNode.GetString() : null;
            if (code is "NoRoute" or "NoSegment") return new DeliveryRouteResult(DeliveryRouteStatus.NoRoute, Provider: "mapbox", RoutingProfile: profile);
            if (code != "Ok" || !root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0) return new DeliveryRouteResult(DeliveryRouteStatus.InvalidResponse, Provider: "mapbox", RoutingProfile: profile);
            var route = routes[0];
            if (!route.TryGetProperty("distance", out var distanceNode) || !distanceNode.TryGetDouble(out var distance) || !double.IsFinite(distance) || distance < 0) return new DeliveryRouteResult(DeliveryRouteStatus.InvalidResponse, Provider: "mapbox", RoutingProfile: profile);
            TimeSpan? duration = null;
            if (route.TryGetProperty("duration", out var durationNode) && durationNode.TryGetDouble(out var seconds) && double.IsFinite(seconds) && seconds >= 0) duration = TimeSpan.FromSeconds(seconds);
            return new DeliveryRouteResult(DeliveryRouteStatus.Success, checked((long)Math.Round(distance, MidpointRounding.AwayFromZero)), duration, "mapbox", profile);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new DeliveryRouteResult(DeliveryRouteStatus.Timeout, Provider: "mapbox", RoutingProfile: profile); }
        catch (HttpRequestException exception) { logger.LogWarning("Mapbox routing transport failure: {FailureType}", exception.HttpRequestError); return new DeliveryRouteResult(DeliveryRouteStatus.ProviderUnavailable, Provider: "mapbox", RoutingProfile: profile); }
        catch (JsonException) { return new DeliveryRouteResult(DeliveryRouteStatus.InvalidResponse, Provider: "mapbox", RoutingProfile: profile); }
        catch (OverflowException) { return new DeliveryRouteResult(DeliveryRouteStatus.InvalidResponse, Provider: "mapbox", RoutingProfile: profile); }
    }

    internal static string BuildUri(DeliveryRouteRequest request, string profile, string token)
    {
        static string Coordinate(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        return $"directions/v5/mapbox/{profile}/{Coordinate(request.Origin.Longitude)},{Coordinate(request.Origin.Latitude)};{Coordinate(request.Destination.Longitude)},{Coordinate(request.Destination.Latitude)}?alternatives=false&overview=false&steps=false&access_token={Uri.EscapeDataString(token)}";
    }

    private TimeSpan ResolveRetryAfter(HttpResponseMessage response, DeliveryRoutingOptions settings)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero) return delta;
        if (response.Headers.RetryAfter?.Date is { } date && date > timeProvider.GetUtcNow()) return date - timeProvider.GetUtcNow();
        return settings.DefaultRateLimitRetryAfter;
    }
    private sealed record CachedRoute(long DistanceMeters, double? DurationSeconds) { public DeliveryRouteResult ToResult(string profile) => new(DeliveryRouteStatus.Success, DistanceMeters, DurationSeconds.HasValue ? TimeSpan.FromSeconds(DurationSeconds.Value) : null, "mapbox", profile); }
}
