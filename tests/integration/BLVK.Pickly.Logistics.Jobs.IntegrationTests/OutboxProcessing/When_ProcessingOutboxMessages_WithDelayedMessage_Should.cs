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
public sealed class When_ProcessingOutboxMessages_WithDelayedMessage_Should
    : JobsWorkerIntegrationTestBase
{
    private readonly ContainersFixture _fixture;
    private readonly Channel<UserCreated> _channel = Channel.CreateUnbounded<UserCreated>();
    private ServiceProvider _subscriberServices = default!;
    private ISubscriber _subscriber = default!;
    private Guid _outboxMessageId;

    public When_ProcessingOutboxMessages_WithDelayedMessage_Should(ContainersFixture fixture)
        : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DispatchOnlyAfterTheConfiguredDeliveryTime()
    {
        var deliverAtUtc = DateTimeOffset.UtcNow.AddSeconds(3);
        await InsertDelayedUserCreatedOutboxMessageAsync(deliverAtUtc);

        using var earlyCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Should.ThrowAsync<OperationCanceledException>(async () => await _channel.Reader.ReadAsync(earlyCts.Token));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var received = await _channel.Reader.ReadAsync(cts.Token);

        received.MessageId.ShouldNotBe(Guid.Empty);
        DateTimeOffset.UtcNow.ShouldBeGreaterThanOrEqualTo(deliverAtUtc);

        await WaitForConditionAsync(async () =>
        {
            await using var scope = CreateDbContextScope();
            var message = await new EfRepository<OutboxMessage>(scope.DbContext).GetByIdAsync(_outboxMessageId);
            return message?.SentAtUtc is not null;
        });
    }

    protected override Task InitializeWorkerTestAsync() => StartRabbitMqSubscriberAsync();

    protected override async Task DisposeWorkerTestAsync()
    {
        await DisposeSubscriberAsync();
        await DeleteOutboxRecordAsync();
    }

    protected override void RegisterWorkers(IServiceCollection services, IConfiguration configuration) =>
        services.AddOutboxMessageProcessing(configuration);

    protected override IReadOnlyDictionary<string, string?> GetAdditionalConfiguration() =>
        new Dictionary<string, string?>
        {
            [$"{OutboxProcessingOptions.SectionName}:PollIntervalSeconds"] = "30"
        };

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
            SubscriptionName = $"jobs-outbox-delayed-{Guid.CreateVersion7():N}",
            ExchangeName = CustomJobsApplicationFactory.EventsExchange,
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

    private async Task InsertDelayedUserCreatedOutboxMessageAsync(DateTimeOffset deliverAtUtc)
    {
        var @event = new UserCreated();
        var outboxMessage = OutboxMessage.CreateEvent(
            @event.MessageId,
            typeof(UserCreated).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(@event),
            @event.OccuredAt,
            deliverAtUtc);

        _outboxMessageId = outboxMessage.Id;

        await using var scope = CreateDbContextScope();
        var repository = new EfRepository<OutboxMessage>(scope.DbContext);
        await repository.AddAsync(outboxMessage);
        await scope.DbContext.SaveChangesAsync();
    }

    private async Task DeleteOutboxRecordAsync()
    {
        if (_outboxMessageId == Guid.Empty)
        {
            return;
        }

        await using var scope = CreateDbContextScope();
        var repository = new EfRepository<OutboxMessage>(scope.DbContext);
        var message = await repository.GetByIdAsync(_outboxMessageId);

        if (message is null)
        {
            return;
        }

        repository.Remove(message);
        await scope.DbContext.SaveChangesAsync();
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
