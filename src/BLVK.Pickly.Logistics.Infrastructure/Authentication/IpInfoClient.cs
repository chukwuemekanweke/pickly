using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal sealed class IpInfoClient(
    IHttpClientFactory httpClientFactory,
    IOptions<IpInfoOptions> options,
    ILogger<IpInfoClient> logger) : IIpGeolocationProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.IpInfo);
    private readonly string _accessToken = options.Value.AccessToken;

    public Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken) =>
        GetGeolocationCoreAsync(ipAddress, cancellationToken);

    private async Task<IpGeolocation?> GetGeolocationCoreAsync(string ipAddress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(BuildRequestUri(ipAddress), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            IpGeolocationLog.ProviderReturnedFailure(logger, nameof(IpInfoClient), response.StatusCode);
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<IpInfoResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return string.IsNullOrWhiteSpace(payload?.Country)
            ? null
            : new IpGeolocation(payload.City, payload.Region, payload.Country);
    }

    private string BuildRequestUri(string ipAddress) =>
        string.IsNullOrWhiteSpace(_accessToken)
            ? $"/{ipAddress}/json"
            : $"/{ipAddress}/json?token={_accessToken}";

    private sealed record IpInfoResponse(string? City, string? Region, string? Country);
}
