using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BLVK.Pickly.Logistics.Consumer;

public sealed class ConsumerLivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("Consumer host is alive."));
}
