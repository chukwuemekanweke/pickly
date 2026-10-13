using BLVK.Pickly.Logistics.Consumer.Storage;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Storage;

public sealed class When_DeletingQuarantinedAvatarObject_WithInvalidCommand_Should
{
    [Fact]
    public async Task RejectWithoutDeletingObject()
    {
        var messageContext = Substitute.For<IMessageContext>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var sut = new DeleteQuarantinedObjectHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            objectStorageService,
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System,
            Substitute.For<IRepository<MessageInbox>>(),
            Substitute.For<ILogger<DeleteQuarantinedObjectHandler>>());
        var command = new DeleteQuarantinedObject(Guid.Empty, string.Empty)
        {
            TenantId = Guid.CreateVersion7()
        };

        var action = () => sut.HandleAsync(command, CancellationToken.None);

        await action.ShouldThrowAsync<CannotProcessMessageNonTransientException>();
        await objectStorageService.DidNotReceiveWithAnyArgs().DeletePrivateObjectAsync(default!, default);
    }
}
