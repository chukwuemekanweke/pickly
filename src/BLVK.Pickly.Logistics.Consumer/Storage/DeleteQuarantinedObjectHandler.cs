using BLVK.Pickly.Logistics.Contracts.Commands.Storage;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Storage;

public sealed class DeleteQuarantinedObjectHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IObjectStorageService objectStorageService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<DeleteQuarantinedObjectHandler> logger)
    : BaseMessageHandler<DeleteQuarantinedObject>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(
        DeleteQuarantinedObject message,
        CancellationToken cancellationToken)
    {
        if (message.UploadId == Guid.Empty || string.IsNullOrWhiteSpace(message.ObjectKey))
        {
            throw new CannotProcessMessageNonTransientException(
                "DeleteQuarantinedObject must contain a valid upload id and object key.");
        }

        await objectStorageService.DeletePrivateObjectAsync(message.ObjectKey, cancellationToken);
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(
        DeleteQuarantinedObject message) =>
        [(Observability.PropertyNames.Common.UploadId, message.UploadId.ToString())];
}
