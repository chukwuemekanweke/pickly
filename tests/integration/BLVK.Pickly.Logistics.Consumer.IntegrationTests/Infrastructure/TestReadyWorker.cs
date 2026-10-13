using Microsoft.Extensions.Hosting;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;

public sealed class TestReadyWorker(WorkerReadinessState readinessState) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
