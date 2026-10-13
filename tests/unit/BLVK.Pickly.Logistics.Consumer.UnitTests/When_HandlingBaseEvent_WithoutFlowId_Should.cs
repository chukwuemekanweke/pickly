using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class When_HandlingBaseEvent_WithoutFlowId_Should
{
    [Fact]
    public async Task SetEmptyFlowId()
    {
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var messageContext = Substitute.For<IMessageContext>();
        var tenantId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7().ToString("N");
        messageContext.CorrelationId.Returns(correlationId);

        await new UserCreatedHandlerForTests(customTelemetryContext, currentActorAccessor, messageContext)
            .HandleAsync(
                new UserCreated
                {
                    TenantId = tenantId,
                    FlowId = null
                },
                CancellationToken.None);

        currentActorAccessor.Received(1).Set(
            Arg.Any<string>(),
            tenantId,
            correlationId,
            string.Empty);
    }

    private sealed class UserCreatedHandlerForTests(
        ICustomTelemetryContext customTelemetryContext,
        ICurrentActorAccessor currentActorAccessor,
        IMessageContext messageContext)
        : BaseMessageHandler<UserCreated>(
            customTelemetryContext,
            currentActorAccessor,
            messageContext,
            Substitute.For<IRepository<MessageInbox>>(),
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System,
            Substitute.For<ILogger>())
    {
        protected override Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
