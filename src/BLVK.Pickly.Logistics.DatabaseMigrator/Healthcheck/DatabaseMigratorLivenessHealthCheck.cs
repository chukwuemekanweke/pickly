using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BLVK.Pickly.Logistics.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigratorLivenessHealthCheck(DatabaseMigrationState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.Status switch
        {
            DatabaseMigrationStatus.Succeeded => HealthCheckResult.Healthy("Database migrator finished all migration and script work successfully."),
            DatabaseMigrationStatus.Failed => HealthCheckResult.Unhealthy(
                "Database migrator failed while running migration or script work.",
                state.Failure),
            DatabaseMigrationStatus.Pending => HealthCheckResult.Unhealthy("Database migrator has not started migration work yet."),
            DatabaseMigrationStatus.Running => HealthCheckResult.Unhealthy("Database migrator is still running migration work."),
            _ => HealthCheckResult.Unhealthy("Database migrator is in an unknown state.")
        });
}
