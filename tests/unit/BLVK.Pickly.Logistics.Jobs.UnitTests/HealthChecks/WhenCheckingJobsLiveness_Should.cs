using BLVK.Pickly.Logistics.Jobs.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace BLVK.Pickly.Logistics.Jobs.UnitTests.HealthChecks;

public sealed class WhenCheckingJobsLiveness_Should
{
    [Fact]
    public async Task ReturnHealthy()
    {
        var healthCheck = new JobsLivenessHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}

