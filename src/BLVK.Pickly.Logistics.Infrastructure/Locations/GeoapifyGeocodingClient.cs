using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

internal sealed class GeoapifyGeocodingClient(
    HttpClient httpClient,
    IOptions<GeoapifyGeocodingOptions> options,
    TimeProvider timeProvider,
    ILogger<GeoapifyGeocodingClient> logger) : IGeocodingProviderClient
{
    public const string Provider = "geoapify";
    public string ProviderName => Provider;
    public LocationResolutionSource ResolutionSource => LocationResolutionSource.Geoapify;

    public async Task<GeocodingResult> ForwardGeocodeAsync(GeocodingRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return Failed(GeocodingStatus.ProviderUnavailable);
        }

        try
        {
            using var response = await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(request, settings.ApiKey)),
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return Failed(GeocodingStatus.RateLimited, ResolveRetryAfter(response, settings));
            }

            if ((int)response.StatusCode >= 500)
            {
                return Failed(GeocodingStatus.ProviderUnavailable);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Failed(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? GeocodingStatus.ProviderUnavailable
                    : GeocodingStatus.InvalidResponse);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var parsed = GeoapifyFeatureParser.Parse(document.RootElement);
            return parsed with
            {
                Candidates = parsed.Candidates.Take(request.MaxCandidates).ToArray(),
                Provider = Provider,
                ResolutionSource = ResolutionSource
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed(GeocodingStatus.Timeout);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning("Geoapify geocoding transport failure: {FailureType}", exception.HttpRequestError);
            return Failed(GeocodingStatus.ProviderUnavailable);
        }
        catch (JsonException)
        {
            return Failed(GeocodingStatus.InvalidResponse);
        }
    }

    internal static string BuildRequestUri(GeocodingRequest request, string apiKey)
    {
        var query = new StringBuilder("v1/geocode/search?");
        if (string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.State))
        {
            query.Append("text=").Append(Uri.EscapeDataString(request.AddressLine));
        }
        else
        {
            query.Append("street=").Append(Uri.EscapeDataString(request.AddressLine))
                .Append("&city=").Append(Uri.EscapeDataString(request.City))
                .Append("&state=").Append(Uri.EscapeDataString(request.State));
        }

        return query.Append("&filter=countrycode:").Append(Uri.EscapeDataString(request.CountryCode.ToLowerInvariant()))
            .Append("&limit=").Append(request.MaxCandidates.ToString(CultureInfo.InvariantCulture))
            .Append("&format=geojson&apiKey=").Append(Uri.EscapeDataString(apiKey))
            .ToString();
    }

    private TimeSpan ResolveRetryAfter(HttpResponseMessage response, GeoapifyGeocodingOptions settings)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date && date > timeProvider.GetUtcNow())
        {
            return date - timeProvider.GetUtcNow();
        }

        return settings.DefaultRateLimitRetryAfter;
    }

    private static GeocodingResult Failed(GeocodingStatus status, TimeSpan? retryAfter = null) =>
        GeocodingResult.Failed(status, retryAfter, Provider, LocationResolutionSource.Geoapify);
}
