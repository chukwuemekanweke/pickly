using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLVK.Pickly.Logistics.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigrationWorker(
    IServiceProvider services,
    IHostEnvironment environment,
    DatabaseMigrationState state,
    ILogger<DatabaseMigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            state.MarkRunning();
            logger.LogInformation("Starting database deployment");
            await services.InitializeDatabaseAsync(environment.ContentRootPath, stoppingToken);
            state.MarkSucceeded();
            logger.LogInformation("Database deployment completed successfully");
        }
        catch (Exception exception)
        {
            state.MarkFailed(exception);
            logger.LogError(exception, "Database deployment failed");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
