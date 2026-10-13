using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal sealed class IpWhoIsClient(
    IHttpClientFactory httpClientFactory,
    IOptions<IpWhoIsOptions> options,
    ILogger<IpWhoIsClient> logger) : IIpGeolocationProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.IpWhoIs);
    private readonly string _apiKey = options.Value.ApiKey;

    public Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken) =>
        GetGeolocationCoreAsync(ipAddress, cancellationToken);

    private async Task<IpGeolocation?> GetGeolocationCoreAsync(string ipAddress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(BuildRequestUri(ipAddress), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            IpGeolocationLog.ProviderReturnedFailure(logger, nameof(IpWhoIsClient), response.StatusCode);
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<IpWhoIsResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return payload is { Success: true }
            ? new IpGeolocation(payload.City, payload.Region, payload.Country)
            : null;
    }

    private string BuildRequestUri(string ipAddress) =>
        string.IsNullOrWhiteSpace(_apiKey)
            ? $"/{ipAddress}"
            : $"/{ipAddress}?key={_apiKey}";

    private sealed record IpWhoIsResponse(bool Success, string? City, string? Region, string? Country);
}
