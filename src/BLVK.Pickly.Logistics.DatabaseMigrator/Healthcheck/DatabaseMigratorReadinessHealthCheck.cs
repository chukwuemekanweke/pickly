using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace BLVK.Pickly.Logistics.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigratorReadinessHealthCheck(
    IConfiguration configuration,
    DatabaseMigrationState state) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (state.Status == DatabaseMigrationStatus.Failed)
        {
            return HealthCheckResult.Unhealthy(
                "Database migrator failed before completing deployment.",
                state.Failure);
        }

        try
        {
            var sqlConnectionString = GetRequiredConnectionString("PostgresWrite");
            await using var sqlConnection = new NpgsqlConnection(sqlConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            await using var sqlCommand = new NpgsqlCommand("SELECT 1", sqlConnection);
            await sqlCommand.ExecuteScalarAsync(cancellationToken);

            return state.Status switch
            {
                DatabaseMigrationStatus.Succeeded => HealthCheckResult.Healthy("Database migrator completed deployment work successfully."),
                DatabaseMigrationStatus.Pending => HealthCheckResult.Unhealthy("Database migrator has not started deployment work yet."),
                DatabaseMigrationStatus.Running => HealthCheckResult.Unhealthy("Database migrator is still running deployment work."),
                _ => HealthCheckResult.Unhealthy("Database migrator is in an unknown state.")
            };
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Database migrator cannot connect to PostgreSQL.",
                exception);
        }
    }

    private string GetRequiredConnectionString(string name) =>
        PostgresConnectionString.Normalize(
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is required."));
}
