using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

/// <summary>
/// Mapbox Geocoding v6 structured forward geocoding in permanent mode (<c>permanent=true</c>). Results requested
/// this way may be stored, which is why this is the only provider path the registry persists from. The client never
/// downgrades to temporary geocoding.
/// </summary>
internal sealed class MapboxGeocodingClient(
    HttpClient httpClient,
    IOptions<MapboxGeocodingOptions> options,
    TimeProvider timeProvider,
    ILogger<MapboxGeocodingClient> logger) : ILocationGeocodingProvider
{
    public const string Provider = "mapbox";
    private const string ForwardPath = "search/geocode/v6/forward";

    public string ProviderName => Provider;
    public LocationResolutionSource ResolutionSource => LocationResolutionSource.Mapbox;

    public async Task<GeocodingResult> ForwardGeocodePermanentAsync(
        GeocodingRequest request,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsPermanentGeocodingAvailable)
        {
            logger.LogWarning(
                "Mapbox permanent geocoding is not enabled; location resolution is refused instead of using temporary results");
            return GeocodingResult.Failed(GeocodingStatus.PermanentStorageUnavailable);
        }

        using var activity = GeocodingTelemetry.ActivitySource.StartActivity("locations.geocoding.mapbox", ActivityKind.Client);
        activity?.SetTag("geocoding.provider", Provider);
        activity?.SetTag("geocoding.storage", "permanent");

        var startedAt = timeProvider.GetTimestamp();
        var result = await SendAsync(request, settings, cancellationToken);
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        activity?.SetTag("geocoding.outcome", result.Status.ToString());
        activity?.SetTag("geocoding.candidate_count", result.Candidates.Count);
        if (result.Status != GeocodingStatus.Success)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.Status.ToString());
        }

        logger.LogInformation(
            "Mapbox permanent geocoding completed with {GeocodingOutcome}, {CandidateCount} candidates in {ElapsedMilliseconds} ms",
            result.Status,
            result.Candidates.Count,
            (long)elapsed.TotalMilliseconds);
        return result;
    }

    private async Task<GeocodingResult> SendAsync(
        GeocodingRequest request,
        MapboxGeocodingOptions settings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(request, settings.AccessToken));
            using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
                    {
                        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                        return MapboxFeatureParser.Parse(document.RootElement);
                    }
                case HttpStatusCode.TooManyRequests:
                    logger.LogWarning("Mapbox geocoding rate limit reached");
                    return GeocodingResult.Failed(GeocodingStatus.RateLimited, ResolveRetryAfter(response, settings));
                case HttpStatusCode.Forbidden:
                    logger.LogError("Mapbox rejected the permanent geocoding request; verify the account permits permanent storage");
                    return GeocodingResult.Failed(GeocodingStatus.PermanentStorageUnavailable);
                case HttpStatusCode.Unauthorized:
                    logger.LogError("Mapbox rejected the configured access token");
                    return GeocodingResult.Failed(GeocodingStatus.ProviderUnavailable);
                case >= HttpStatusCode.InternalServerError:
                    logger.LogWarning("Mapbox geocoding is unavailable with status {StatusCode}", (int)response.StatusCode);
                    return GeocodingResult.Failed(GeocodingStatus.ProviderUnavailable);
                default:
                    logger.LogWarning("Mapbox geocoding rejected the request with status {StatusCode}", (int)response.StatusCode);
                    return GeocodingResult.Failed(GeocodingStatus.InvalidResponse);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Mapbox geocoding timed out after {TimeoutSeconds} seconds", settings.Timeout.TotalSeconds);
            return GeocodingResult.Failed(GeocodingStatus.Timeout);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("Mapbox geocoding transport failure: {FailureType}", exception.HttpRequestError);
            return GeocodingResult.Failed(GeocodingStatus.ProviderUnavailable);
        }
        catch (JsonException)
        {
            logger.LogWarning("Mapbox geocoding returned malformed JSON");
            return GeocodingResult.Failed(GeocodingStatus.InvalidResponse);
        }
    }

    internal static string BuildRequestUri(GeocodingRequest request, string accessToken)
    {
        var query = new StringBuilder(ForwardPath)
            .Append("?address_line1=").Append(Uri.EscapeDataString(request.AddressLine))
            .Append("&place=").Append(Uri.EscapeDataString(request.City))
            .Append("&region=").Append(Uri.EscapeDataString(request.State))
            .Append("&country=").Append(Uri.EscapeDataString(request.CountryCode.ToLowerInvariant()))
            .Append("&permanent=true")
            .Append("&autocomplete=false")
            .Append("&limit=").Append(request.MaxCandidates.ToString(CultureInfo.InvariantCulture))
            .Append("&access_token=").Append(Uri.EscapeDataString(accessToken));
        return query.ToString();
    }

    private TimeSpan ResolveRetryAfter(HttpResponseMessage response, MapboxGeocodingOptions settings)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date && date > timeProvider.GetUtcNow())
        {
            return date - timeProvider.GetUtcNow();
        }

        // Mapbox reports the window reset as a Unix timestamp in X-Rate-Limit-Reset.
        if (response.Headers.TryGetValues("X-Rate-Limit-Reset", out var values) &&
            long.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var resetSeconds))
        {
            var wait = DateTimeOffset.FromUnixTimeSeconds(resetSeconds) - timeProvider.GetUtcNow();
            if (wait > TimeSpan.Zero && wait <= TimeSpan.FromMinutes(5))
            {
                return wait;
            }
        }

        return settings.DefaultRateLimitRetryAfter;
    }
}
