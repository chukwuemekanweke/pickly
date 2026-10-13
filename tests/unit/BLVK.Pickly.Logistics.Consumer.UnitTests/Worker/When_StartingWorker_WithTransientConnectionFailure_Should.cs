using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Worker;

public sealed class When_StartingWorker_WithTransientConnectionFailure_Should
{
    [Fact]
    public async Task RetryAndMarkReady()
    {
        var subscriber = Substitute.For<ISubscriber>();
        var consumer = Substitute.For<IConsumer>();
        var logger = Substitute.For<ILogger<global::BLVK.Pickly.Logistics.Consumer.Worker>>();
        var readinessState = new WorkerReadinessState();
        var timeProvider = TimeProvider.System;
        var cts = new CancellationTokenSource();

        var attemptCount = 0;
        subscriber.StartAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new Exception("Connection failed");
            }

            return Task.CompletedTask;
        });

        var worker = new global::BLVK.Pickly.Logistics.Consumer.Worker(
            subscriber, consumer, logger, readinessState, timeProvider);

        await worker.StartAsync(cts.Token);
        await WaitForConditionAsync(() => readinessState.IsReady);

        readinessState.IsReady.ShouldBeTrue();
        attemptCount.ShouldBe(2);

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException("Condition not met in time.");
    }
}
