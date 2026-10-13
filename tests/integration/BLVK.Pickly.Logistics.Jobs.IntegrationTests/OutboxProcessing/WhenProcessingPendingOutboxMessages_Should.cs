using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Jobs.OutboxProcessing;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;
using System.Threading.Channels;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.OutboxProcessing;

[Collection(nameof(ContainersCollection))]
public sealed class WhenProcessingPendingOutboxMessages_Should
    : JobsWorkerIntegrationTestBase
{
    private const string EventsExchange = "x.events.blvkpicklylogistics.integrationtests";
    private readonly ContainersFixture _fixture;
    private readonly Channel<UserCreated> _channel = Channel.CreateUnbounded<UserCreated>();
    private ServiceProvider _subscriberServices = default!;
    private ISubscriber _subscriber = default!;
    private Guid _outboxMessageId;
    private Guid _stakeholderId;

    public WhenProcessingPendingOutboxMessages_Should(ContainersFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MarkMessageAsSent()
    {
        await WhenTheJobsWorkerProcessesPendingOutboxMessages();
        await ThenThePublishedMessageIsReceivedAndTheOutboxMessageIsMarkedAsSent();

        async Task WhenTheJobsWorkerProcessesPendingOutboxMessages()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var received = await _channel.Reader.ReadAsync(cts.Token);

            received.StakeholderId.ShouldBe(_stakeholderId);
            received.MessageId.ShouldNotBe(Guid.Empty);

            await WaitForConditionAsync(async () =>
            {
                await using var scope = CreateDbContextScope();
                var dbContext = scope.DbContext;
                var repository = new EfRepository<OutboxMessage>(dbContext);
                var message = await repository.GetByIdAsync(_outboxMessageId);
                return message?.SentAtUtc is not null;
            });
        }

        async Task ThenThePublishedMessageIsReceivedAndTheOutboxMessageIsMarkedAsSent()
        {
            await using var scope = CreateDbContextScope();
            var dbContext = scope.DbContext;
            var repository = new EfRepository<OutboxMessage>(dbContext);
            var message = await repository.GetByIdAsync(_outboxMessageId);

            message.ShouldNotBeNull();
            message.SentAtUtc.ShouldNotBeNull();
            message.AttemptCount.ShouldBe(0);
            message.LastError.ShouldBeNull();
        }
    }

    protected override async Task InitializeWorkerTestAsync()
    {
        await StartRabbitMqSubscriberAsync();
        await SeedPendingUserCreatedOutboxMessageAsync();
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        await DisposeSubscriberAsync();
        await DeleteOutboxRecordsAsync();
    }

    protected override void RegisterWorkers(IServiceCollection services, IConfiguration configuration) =>
        services.AddOutboxMessageProcessing(configuration);

    private async Task StartRabbitMqSubscriberAsync()
    {
        var subscriberConfig = new SubscriberConfig
        {
            ServiceName = "BLVK.Pickly.Logistics.Jobs.IntegrationTests",
            HostName = _fixture.RabbitMqHostName,
            Port = _fixture.RabbitMqPort,
            UserName = _fixture.RabbitMqUserName,
            Password = _fixture.RabbitMqPassword,
            VirtualHost = _fixture.RabbitMqVirtualHost,
            SubscriptionName = $"jobs-outbox-{Guid.CreateVersion7():N}",
            ExchangeName = EventsExchange,
            PrefetchCount = 1,
            MaxRetryCount = 10,
            ConcurrentMessageCount = 1
        };

        _subscriberServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_channel)
            .AddSubscriber(subscriberConfig, builder => builder.AddHandler<UserCreated, CapturingUserCreatedHandler>())
            .BuildServiceProvider();

        _subscriber = _subscriberServices.GetRequiredKeyedService<ISubscriber>(subscriberConfig.Key);
        await _subscriber.StartAsync(CancellationToken.None);
    }

    private async Task SeedPendingUserCreatedOutboxMessageAsync()
    {
        _stakeholderId = Guid.CreateVersion7();

        var @event = new UserCreated
        {
            StakeholderId = _stakeholderId
        };
        var outboxMessage = OutboxMessage.CreateEvent(
            @event.MessageId,
            typeof(UserCreated).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(@event),
            @event.OccuredAt,
            @event.OccuredAt);

        _outboxMessageId = outboxMessage.Id;

        await using var scope = CreateDbContextScope();
        var dbContext = scope.DbContext;
        var repository = new EfRepository<OutboxMessage>(dbContext);
        await repository.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();
    }

    private async Task DeleteOutboxRecordsAsync()
    {
        if (_outboxMessageId == Guid.Empty)
        {
            return;
        }

        await using var scope = CreateDbContextScope();
        var dbContext = scope.DbContext;
        var repository = new EfRepository<OutboxMessage>(dbContext);
        var message = await repository.GetByIdAsync(_outboxMessageId);

        if (message is null)
        {
            return;
        }

        repository.Remove(message);
        await dbContext.SaveChangesAsync();
    }

    private async Task DisposeSubscriberAsync()
    {
        if (_subscriber is not null)
        {
            await _subscriber.StopAsync(CancellationToken.None);
        }

        if (_subscriberServices is not null)
        {
            await _subscriberServices.DisposeAsync();
        }
    }

    private sealed class CapturingUserCreatedHandler(Channel<UserCreated> channel) : IMessageHandler<UserCreated>
    {
        public Task HandleAsync(UserCreated message, CancellationToken cancellationToken) =>
            channel.Writer.WriteAsync(message, cancellationToken).AsTask();
    }
}

