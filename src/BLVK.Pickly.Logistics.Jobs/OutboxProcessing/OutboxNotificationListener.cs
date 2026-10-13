using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Npgsql;

namespace BLVK.Pickly.Logistics.Jobs.OutboxProcessing;

public sealed class OutboxNotificationListener(
    IConfiguration configuration,
    ILogger<OutboxNotificationListener> logger,
    OutboxProcessingSignal signal) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("PostgresWrite")
            ?? throw new InvalidOperationException("Connection string 'PostgresWrite' is required for outbox notifications.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(PostgresConnectionString.Normalize(connectionString));
                connection.Notification += HandleNotification;

                await connection.OpenAsync(stoppingToken);

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"LISTEN {OutboxProcessingOptions.NotificationChannel};";
                    await command.ExecuteNonQueryAsync(stoppingToken);
                }

                signal.Pulse();

                while (!stoppingToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox notification listener failed. Reconnecting in {DelaySeconds}s.", ReconnectDelay.TotalSeconds);
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private void HandleNotification(object? sender, NpgsqlNotificationEventArgs args)
    {
        if (!string.Equals(args.Channel, OutboxProcessingOptions.NotificationChannel, StringComparison.Ordinal))
        {
            return;
        }

        signal.Pulse();
    }
}
