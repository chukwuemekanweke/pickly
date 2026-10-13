using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal sealed class IpApiComClient(
    IHttpClientFactory httpClientFactory,
    ILogger<IpApiComClient> logger) : IIpGeolocationProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.IpApiCom);

    public Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken) =>
        GetGeolocationCoreAsync(ipAddress, cancellationToken);

    private async Task<IpGeolocation?> GetGeolocationCoreAsync(string ipAddress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"/json/{ipAddress}?fields=status,country,regionName,city", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            IpGeolocationLog.ProviderReturnedFailure(logger, nameof(IpApiComClient), response.StatusCode);
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<IpApiComResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return payload is { Status: "success" }
            ? new IpGeolocation(payload.City, payload.RegionName, payload.Country)
            : null;
    }

    private sealed record IpApiComResponse(string Status, string? Country, string? RegionName, string? City);
}
