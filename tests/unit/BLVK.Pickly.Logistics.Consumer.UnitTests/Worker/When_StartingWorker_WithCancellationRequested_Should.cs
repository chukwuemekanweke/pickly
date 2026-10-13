using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Worker;

public sealed class When_StartingWorker_WithCancellationRequested_Should
{
    [Fact]
    public async Task NotMarkReady()
    {
        var subscriber = Substitute.For<ISubscriber>();
        var consumer = Substitute.For<IConsumer>();
        var logger = Substitute.For<ILogger<global::BLVK.Pickly.Logistics.Consumer.Worker>>();
        var readinessState = new WorkerReadinessState();
        var timeProvider = TimeProvider.System;

        subscriber.StartAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new Exception("Connection failed"));

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var worker = new global::BLVK.Pickly.Logistics.Consumer.Worker(
            subscriber, consumer, logger, readinessState, timeProvider);

        await worker.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(2));

        readinessState.IsReady.ShouldBeFalse();

        await worker.StopAsync(CancellationToken.None);
    }
}
