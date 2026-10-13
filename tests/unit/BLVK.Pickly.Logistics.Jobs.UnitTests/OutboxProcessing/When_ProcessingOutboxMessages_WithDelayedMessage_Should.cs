using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using BLVK.Pickly.Logistics.Jobs.OutboxProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Jobs.UnitTests.OutboxProcessing;

public sealed class When_ProcessingOutboxMessages_WithDelayedMessage_Should
{
    [Fact]
    public async Task SkipDispatchingUntilTheMessageIsDue()
    {
        var delayedMessage = OutboxMessage.CreateEvent(
            Guid.CreateVersion7(),
            "type",
            "{}",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddSeconds(10));
        var repository = new InMemoryOutboxRepository(delayedMessage);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dispatcher = Substitute.For<IOutboxMessageDispatcher>();
        var signal = new OutboxProcessingSignal();
        var state = new BackgroundServiceReadinessState([new BackgroundServiceDescriptor(OutboxMessageProcessor.ServiceName)]);
        var services = new ServiceCollection()
            .AddSingleton<IRepository<OutboxMessage>>(repository)
            .AddSingleton(unitOfWork)
            .AddSingleton(dispatcher)
            .BuildServiceProvider();

        var sut = new OutboxMessageProcessor(
            NullLogger<OutboxMessageProcessor>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new OutboxProcessingOptions { BatchSize = 10, PollIntervalSeconds = 1 }),
            signal,
            state);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await sut.StopAsync(CancellationToken.None);

        delayedMessage.SentAtUtc.ShouldBeNull();
        await dispatcher.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class InMemoryOutboxRepository(params OutboxMessage[] messages) : IRepository<OutboxMessage>
    {
        private readonly List<OutboxMessage> _messages = [.. messages];

        public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_messages.SingleOrDefault(message => message.Id == id));

        public Task<OutboxMessage?> FirstOrDefaultAsync(ISpecification<OutboxMessage> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(SpecificationEvaluator.GetQuery(_messages.AsQueryable(), specification).FirstOrDefault());

        public Task<IReadOnlyList<OutboxMessage>> ListAsync(ISpecification<OutboxMessage> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessage>>(SpecificationEvaluator.GetQuery(_messages.AsQueryable(), specification).ToList());

        public Task<bool> AnyAsync(ISpecification<OutboxMessage> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(SpecificationEvaluator.GetQuery(_messages.AsQueryable(), specification).Any());

        public Task<int> CountAsync(ISpecification<OutboxMessage> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(SpecificationEvaluator.GetQuery(_messages.AsQueryable(), specification).Count());

        public Task AddAsync(OutboxMessage entity, CancellationToken cancellationToken = default)
        {
            _messages.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(OutboxMessage entity)
        {
        }

        public void Remove(OutboxMessage entity) => _messages.Remove(entity);
    }
}
