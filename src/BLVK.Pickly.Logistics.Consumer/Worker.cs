using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer;

public sealed class Worker(
    ISubscriber subscriber,
    IConsumer consumer,
    ILogger<Worker> logger,
    WorkerReadinessState readinessState,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting RabbitMQ subscriber and command consumer.");
        await StartWithRetryAsync(stoppingToken);
        readinessState.MarkReady();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            logger.LogInformation("Stopping RabbitMQ subscriber and command consumer.");
            await consumer.StopAsync(CancellationToken.None);
            await subscriber.StopAsync(CancellationToken.None);
        }
    }

    private async Task StartWithRetryAsync(CancellationToken stoppingToken)
    {
        var delay = InitialRetryDelay;
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await subscriber.StartAsync(stoppingToken);
                await consumer.StartAsync(stoppingToken);
                logger.LogInformation("RabbitMQ subscriber and command consumer started successfully.");
                return;
            }
            catch (Exception exception)
            {
                attempt++;
                logger.LogError(
                    exception,
                    "Failed to start RabbitMQ subscriber and command consumer (attempt {Attempt}). Retrying in {DelaySeconds}s...",
                    attempt,
                    delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxRetryDelay.Ticks));
            }
        }

        stoppingToken.ThrowIfCancellationRequested();
    }
}
