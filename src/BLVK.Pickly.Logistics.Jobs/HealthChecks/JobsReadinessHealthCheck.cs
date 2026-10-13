using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using StackExchange.Redis;

namespace BLVK.Pickly.Logistics.Jobs.HealthChecks;

public sealed class JobsReadinessHealthCheck(
    IConfiguration configuration,
    BackgroundServiceReadinessState readinessState) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!readinessState.IsReady)
        {
            var pendingServices = readinessState.PendingServices;
            var description = pendingServices.Count == 0
                ? "No Jobs background services are registered for readiness tracking."
                : $"Jobs background services have not started yet: {string.Join(", ", pendingServices)}.";

            return HealthCheckResult.Unhealthy(description);
        }

        try
        {
            var sqlConnectionString = GetRequiredConnectionString("PostgresWrite");
            var redisConnectionString = GetRequiredConnectionString("Redis");
            await using var sqlConnection = new NpgsqlConnection(sqlConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            await using var sqlCommand = new NpgsqlCommand("SELECT 1", sqlConnection);
            await sqlCommand.ExecuteScalarAsync(cancellationToken);

            await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            await redis.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy("Jobs host can connect to PostgreSQL and Redis.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Jobs host cannot connect to one or more required dependencies.",
                exception);
        }
    }

    private string GetRequiredConnectionString(string name) =>
        PostgresConnectionString.Normalize(
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is required."));
}
