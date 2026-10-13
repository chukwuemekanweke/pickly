using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class When_HandlingMessage_WithoutInboxEntry_Should
{
    [Fact]
    public async Task ProcessAndRecordMessage()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 7, 10, 22, 0, 0, TimeSpan.Zero));
        var tenantId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        MessageInbox? capturedInbox = null;
        var handler = new TestEventHandler(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            messageInboxRepository,
            unitOfWork,
            timeProvider,
            Substitute.For<ILogger>());

        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        messageInboxRepository.FirstOrDefaultAsync(
                Arg.Any<MessageInboxByMessageIdAndTypeSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns((MessageInbox?)null);
        messageInboxRepository.AddAsync(Arg.Do<MessageInbox>(inbox => capturedInbox = inbox), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(new TestEvent { MessageId = messageId, TenantId = tenantId }, CancellationToken.None);

        handler.HandledCount.ShouldBe(1);
        capturedInbox.ShouldNotBeNull();
        capturedInbox.MessageId.ShouldBe(messageId);
        capturedInbox.MessageType.ShouldBe(typeof(TestEvent).FullName);
        capturedInbox.ReceivedAtUtc.ShouldBe(timeProvider.GetUtcNow());
        messageInboxRepository.DidNotReceive().Update(Arg.Any<MessageInbox>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    public sealed record TestEvent : BaseEvent;

    private sealed class TestEventHandler(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext,
        IRepository<MessageInbox> messageInboxRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger logger)
        : BaseMessageHandler<TestEvent>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
    {
        public int HandledCount { get; private set; }

        protected override Task HandleAsyncInternal(TestEvent message, CancellationToken cancellationToken)
        {
            HandledCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
