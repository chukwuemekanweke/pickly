namespace BLVK.Pickly.Logistics.Jobs.HealthChecks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobsHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<JobsReadinessHealthCheck>(
                "jobs-readiness",
                tags: ["readiness"])
            .AddCheck<JobsLivenessHealthCheck>(
                "jobs-liveness",
                tags: ["liveness"]);

        return services;
    }
}
