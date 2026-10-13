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
}
