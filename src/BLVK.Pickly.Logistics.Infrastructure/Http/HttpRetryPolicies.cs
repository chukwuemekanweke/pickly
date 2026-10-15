using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace BLVK.Pickly.Logistics.Infrastructure.Http;

internal static class HttpRetryPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreateTransientRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    var exponentialBackoff = TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));
                    return exponentialBackoff + jitter;
                });

    /// <summary>
    /// Retries only transient transport and 5xx/408 failures. 429 is deliberately not retried so rate limiting is
    /// surfaced to callers with its retry hint instead of being amplified.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> CreateServerErrorRetryPolicy(int retryCount) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    var exponentialBackoff = TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));
                    return exponentialBackoff + jitter;
                });
}
