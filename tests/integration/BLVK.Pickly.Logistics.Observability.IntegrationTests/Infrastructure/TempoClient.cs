using System.Net;

namespace BLVK.Pickly.Logistics.Observability.IntegrationTests.Infrastructure;

public sealed class TempoClient(string baseUrl) : IDisposable
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(baseUrl) };

    public async Task<bool> WaitForTraceAsync(string traceId, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await TraceExistsAsync(traceId))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return await TraceExistsAsync(traceId);
    }

    public async Task<bool> ConfirmTraceRemainsAbsentAsync(string traceId, TimeSpan settleWindow)
    {
        await Task.Delay(settleWindow);
        return !await TraceExistsAsync(traceId);
    }

    private async Task<bool> TraceExistsAsync(string traceId)
    {
        using var response = await _httpClient.GetAsync($"/api/traces/{traceId}");
        return response.StatusCode == HttpStatusCode.OK;
    }

    public void Dispose() => _httpClient.Dispose();
}
