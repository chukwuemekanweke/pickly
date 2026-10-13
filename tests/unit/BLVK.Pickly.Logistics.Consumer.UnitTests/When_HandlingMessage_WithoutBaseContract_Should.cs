using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class When_HandlingMessage_WithoutBaseContract_Should
{
    [Fact]
    public async Task ThrowCannotProcessMessageNonTransientException()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            new TestMessageHandler(customTelemetryContext, currentActorAccessor, messageContext)
                .HandleAsync(new TestMessage(), CancellationToken.None));

        exception.Message.ShouldBe("TestMessage must inherit from BaseCommand or BaseEvent.");
    }

    public sealed record TestMessage;

    private sealed class TestMessageHandler(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext)
        : BaseMessageHandler<TestMessage>(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            Substitute.For<IRepository<MessageInbox>>(),
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System,
            Substitute.For<ILogger>())
    {
        protected override Task HandleAsyncInternal(TestMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
