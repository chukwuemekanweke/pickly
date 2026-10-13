using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using BLVK.Pickly.Logistics.Jobs.OutboxProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Jobs.UnitTests.OutboxProcessing;

public sealed class When_ProcessingOutboxMessages_WithNoPendingMessages_Should
{
    [Fact]
    public async Task SkipSavingChanges()
    {
        var repository = Substitute.For<IRepository<OutboxMessage>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        var signal = new OutboxProcessingSignal();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(OutboxMessageProcessor.ServiceName)]);
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .AddSingleton(unitOfWork)
            .AddSingleton(dispatcher)
            .BuildServiceProvider();

        repository.ListAsync(Arg.Any<ISpecification<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>());
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns((OutboxMessage?)null);

        var sut = new OutboxMessageProcessor(
            NullLogger<OutboxMessageProcessor>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new OutboxProcessingOptions { BatchSize = 10, PollIntervalSeconds = 1 }),
            signal,
            state);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
