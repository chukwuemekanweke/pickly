using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public sealed class UserEmailConfirmedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UserEmailConfirmedHandler> logger,
    IRepository<MessageInbox> messageInboxRepository) : BaseMessageHandler<UserEmailConfirmed>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    protected override Task HandleAsyncInternal(UserEmailConfirmed message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processed user email confirmed event for stakeholder {StakeholderId}.",
            message.StakeholderId);

        if (message.StakeholderId.HasValue)
        {
            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, message.StakeholderId.Value.ToString());
        }

        return Task.CompletedTask;
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserEmailConfirmed message)
    {
        yield break;
    }
}
