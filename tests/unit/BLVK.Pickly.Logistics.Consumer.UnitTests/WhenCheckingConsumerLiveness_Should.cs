using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenCheckingConsumerLiveness_Should
{
    [Fact]
    public async Task ReturnHealthy()
    {
        var healthCheck = new ConsumerLivenessHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}

