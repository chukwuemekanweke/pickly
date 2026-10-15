using System.Globalization;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

/// <summary>Records every provider HTTP attempt, including attempts made by the retry policy.</summary>
internal sealed class GeocodingAttemptTelemetryHandler(TimeProvider timeProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var outcome = "transport_error";
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            outcome = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            return response;
        }
        catch (OperationCanceledException)
        {
            outcome = cancellationToken.IsCancellationRequested ? "cancelled" : "timeout";
            throw;
        }
        finally
        {
            GeocodingTelemetry.RecordAttempt(
                MapboxGeocodingClient.Provider,
                outcome,
                timeProvider.GetElapsedTime(startedAt));
        }
    }
}
