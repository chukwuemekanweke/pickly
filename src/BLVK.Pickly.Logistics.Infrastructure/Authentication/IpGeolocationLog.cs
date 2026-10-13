using Microsoft.Extensions.Logging;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal static partial class IpGeolocationLog
{
    public static void ProviderReturnedFailure(ILogger logger, string provider, HttpStatusCode statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                ProviderAuthenticationFailed(logger, provider, (int)statusCode);
                break;

            case HttpStatusCode.TooManyRequests:
                ProviderRateLimited(logger, provider, (int)statusCode);
                break;

            default:
                if ((int)statusCode >= 500)
                {
                    ProviderServerFailed(logger, provider, (int)statusCode);
                }

                break;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "IP geolocation provider {Provider} timed out. Trying the next provider.")]
    public static partial void ProviderTimedOut(ILogger logger, Exception exception, string provider);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "IP geolocation provider {Provider} encountered a transport failure. Trying the next provider.")]
    public static partial void ProviderTransportFailed(ILogger logger, Exception exception, string provider);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "IP geolocation provider {Provider} returned authentication failure status code {StatusCode}. Trying the next provider.")]
    private static partial void ProviderAuthenticationFailed(ILogger logger, string provider, int statusCode);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "IP geolocation provider {Provider} returned rate-limit status code {StatusCode}. Trying the next provider.")]
    private static partial void ProviderRateLimited(ILogger logger, string provider, int statusCode);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "IP geolocation provider {Provider} returned server error status code {StatusCode}. Trying the next provider.")]
    private static partial void ProviderServerFailed(ILogger logger, string provider, int statusCode);
}
