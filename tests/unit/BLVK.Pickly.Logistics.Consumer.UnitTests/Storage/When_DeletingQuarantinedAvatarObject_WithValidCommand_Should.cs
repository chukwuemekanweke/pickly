using BLVK.Pickly.Logistics.Consumer.Storage;
using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Storage;

public sealed class When_DeletingQuarantinedAvatarObject_WithValidCommand_Should
{
    [Fact]
    public async Task DeleteObjectAndRecordInboxMessage()
    {
        var messageContext = Substitute.For<IMessageContext>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var messageInboxRepository = Substitute.For<IRepository<MessageInbox>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var uploadId = Guid.CreateVersion7();
        var objectKey = $"quarantine/avatars/{uploadId:N}.png";
        var command = new DeleteQuarantinedObject(uploadId, objectKey)
        {
            StakeholderId = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7()
        };
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        var sut = new DeleteQuarantinedObjectHandler(
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<ICurrentActorAccessor>(),
            messageContext,
            objectStorageService,
            unitOfWork,
            TimeProvider.System,
            messageInboxRepository,
            Substitute.For<ILogger<DeleteQuarantinedObjectHandler>>());

        await sut.HandleAsync(command, CancellationToken.None);

        await objectStorageService.Received(1).DeletePrivateObjectAsync(objectKey, Arg.Any<CancellationToken>());
        await messageInboxRepository.Received(1).AddAsync(
            Arg.Is<MessageInbox>(inbox => inbox.MessageId == command.MessageId),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
