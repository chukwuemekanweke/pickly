using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal sealed class GeoapifyDeliveryRouteProvider(
    HttpClient httpClient,
    IOptions<GeoapifyDeliveryRoutingOptions> options,
    TimeProvider timeProvider,
    ILogger<GeoapifyDeliveryRouteProvider> logger) : IRoutingProviderClient
{
    public const string Provider = "geoapify";
    private static readonly HashSet<string> Profiles = ["driving", "driving-traffic", "walking", "cycling"];

    public string ProviderName => Provider;
    public bool SupportsProfile(string profile) => Profiles.Contains(profile);

    public async Task<DeliveryRouteResult> GetRouteAsync(DeliveryRouteRequest request, CancellationToken cancellationToken)
    {
        var profile = request.RoutingProfile.Trim().ToLowerInvariant();
        if (!SupportsProfile(profile))
        {
            return Result(DeliveryRouteStatus.UnsupportedProfile, profile);
        }

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return Result(DeliveryRouteStatus.MissingConfiguration, profile);
        }

        try
        {
            using var response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, BuildUri(request, profile, settings.ApiKey)),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return Result(DeliveryRouteStatus.RateLimited, profile, ResolveRetryAfter(response, settings));
            }

            if ((int)response.StatusCode >= 500)
            {
                return Result(DeliveryRouteStatus.ProviderUnavailable, profile);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Result(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? DeliveryRouteStatus.MissingConfiguration
                    : DeliveryRouteStatus.InvalidResponse, profile);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (!root.TryGetProperty("results", out var routes) || routes.ValueKind != JsonValueKind.Array)
            {
                return Result(DeliveryRouteStatus.InvalidResponse, profile);
            }

            if (routes.GetArrayLength() == 0)
            {
                return Result(DeliveryRouteStatus.NoRoute, profile);
            }

            var route = routes[0];
            if (!TryNonNegativeDouble(route, "distance", out var distance))
            {
                return Result(DeliveryRouteStatus.InvalidResponse, profile);
            }

            if (!TryNonNegativeDouble(route, "time", out var seconds))
            {
                return Result(DeliveryRouteStatus.InvalidResponse, profile);
            }

            return new DeliveryRouteResult(
                DeliveryRouteStatus.Success,
                checked((long)Math.Round(distance, MidpointRounding.AwayFromZero)),
                TimeSpan.FromSeconds(seconds),
                Provider,
                profile);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result(DeliveryRouteStatus.Timeout, profile);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("Geoapify routing transport failure: {FailureType}", exception.HttpRequestError);
            return Result(DeliveryRouteStatus.ProviderUnavailable, profile);
        }
        catch (JsonException)
        {
            return Result(DeliveryRouteStatus.InvalidResponse, profile);
        }
        catch (OverflowException)
        {
            return Result(DeliveryRouteStatus.InvalidResponse, profile);
        }
    }

    internal static string BuildUri(DeliveryRouteRequest request, string profile, string apiKey)
    {
        static string Coordinate(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        var mode = profile switch
        {
            "walking" => "walk",
            "cycling" => "bicycle",
            _ => "drive"
        };
        var traffic = profile == "driving-traffic" ? "&traffic=approximated" : string.Empty;
        return $"v1/routing?waypoints={Coordinate(request.Origin.Latitude)},{Coordinate(request.Origin.Longitude)}|{Coordinate(request.Destination.Latitude)},{Coordinate(request.Destination.Longitude)}&mode={mode}{traffic}&format=json&apiKey={Uri.EscapeDataString(apiKey)}";
    }

    private TimeSpan ResolveRetryAfter(HttpResponseMessage response, GeoapifyDeliveryRoutingOptions settings)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero) return delta;
        if (response.Headers.RetryAfter?.Date is { } date && date > timeProvider.GetUtcNow()) return date - timeProvider.GetUtcNow();
        return settings.DefaultRateLimitRetryAfter;
    }

    private static bool TryNonNegativeDouble(JsonElement element, string name, out double value)
    {
        value = 0;
        return element.TryGetProperty(name, out var node) && node.TryGetDouble(out value) && double.IsFinite(value) && value >= 0;
    }

    private static DeliveryRouteResult Result(DeliveryRouteStatus status, string profile, TimeSpan? retryAfter = null) =>
        new(status, Provider: Provider, RoutingProfile: profile, RetryAfter: retryAfter);
}
