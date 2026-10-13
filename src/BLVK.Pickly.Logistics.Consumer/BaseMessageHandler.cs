using BLVK.Pickly.Logistics.Contracts.Commands;
using BLVK.Pickly.Logistics.Contracts.Common;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Messaging.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.Consumer;

public abstract class BaseMessageHandler<TMessage>(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<MessageInbox> messageInboxRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger logger) : IMessageHandler<TMessage>
{
    private static readonly ActivitySource ActivitySource = new(Observability.ActivitySourceName);

    protected ICustomTelemetryContext CustomTelemetryContext { get; } = customTelemetryContext;

    protected ICurrentActorAccessor ActorAccessor => currentActorAccessor;

    protected TimeProvider Clock => timeProvider;

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity($"{typeof(TMessage).Name}_process", ActivityKind.Consumer);

        try
        {
            await HandleAsyncCore(message, cancellationToken);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);
            throw;
        }
    }

    private async Task HandleAsyncCore(TMessage message, CancellationToken cancellationToken)
    {
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.MessageType, typeof(TMessage).Name);
        var messageId = GetMessageId(message);
        var messageType = GetMessageType();

        if (message is BaseEvent baseEvent)
        {
            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            var tenantId = baseEvent.TenantId == Guid.Empty ? (Guid?)null : baseEvent.TenantId;
            currentActorAccessor.Set(
                baseEvent.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                tenantId,
                messageContext.CorrelationId,
                baseEvent.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.PropertyNames.Common.MessageId, baseEvent.MessageId.ToString())
                .SetProperty(Observability.PropertyNames.Common.OccurredAt, baseEvent.OccuredAt.ToString("O"))
                .SetProperty(Observability.PropertyNames.Common.StakeholderId, baseEvent.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.TenantId, tenantId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.CorrelationId, messageContext.CorrelationId)
                .SetProperty(Observability.PropertyNames.Common.FlowId, baseEvent.FlowId ?? string.Empty);
        }
        else if (message is BaseCommand baseCommand)
        {
            if (baseCommand.TenantId == Guid.Empty)
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a valid tenant id.");
            }

            if (string.IsNullOrWhiteSpace(messageContext.CorrelationId))
            {
                throw new CannotProcessMessageNonTransientException($"{typeof(TMessage).Name} must contain a correlation id header.");
            }

            currentActorAccessor.Set(
                baseCommand.StakeholderId?.ToString() ?? ActorDefaults.SystemActorId,
                baseCommand.TenantId,
                messageContext.CorrelationId,
                baseCommand.FlowId ?? string.Empty);

            CustomTelemetryContext
                .SetProperty(Observability.PropertyNames.Common.MessageId, baseCommand.MessageId.ToString())
                .SetProperty(Observability.PropertyNames.Common.RequestedAt, baseCommand.RequestedAt.ToString("O"))
                .SetProperty(Observability.PropertyNames.Common.StakeholderId, baseCommand.StakeholderId?.ToString() ?? string.Empty)
                .SetProperty(Observability.PropertyNames.Common.TenantId, baseCommand.TenantId.ToString())
                .SetProperty(Observability.PropertyNames.Common.CorrelationId, messageContext.CorrelationId)
                .SetProperty(Observability.PropertyNames.Common.FlowId, baseCommand.FlowId ?? string.Empty);
        }
        else
        {
            throw new CannotProcessMessageNonTransientException(
                $"{typeof(TMessage).Name} must inherit from {nameof(BaseCommand)} or {nameof(BaseEvent)}.");
        }

        var existingInbox = await messageInboxRepository.FirstOrDefaultAsync(
            new MessageInboxByMessageIdAndTypeSpecification(messageId, messageType),
            cancellationToken);
        if (existingInbox is not null)
        {
            logger.LogWarning(
                "Skipping duplicate message {MessageId} of type {MessageType} because it already exists in the inbox.",
                messageId,
                messageType);

            return;
        }

        foreach (var telemetryParameter in GetTelemetryParameters(message))
        {
            CustomTelemetryContext.SetProperty(telemetryParameter.Key, telemetryParameter.Value);
        }

        await HandleAsyncInternal(message, cancellationToken);

        var inbox = MessageInbox.Create(messageId, messageType, timeProvider.GetUtcNow());
        await messageInboxRepository.AddAsync(inbox, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    protected virtual IEnumerable<(string Key, string Value)> GetTelemetryParameters(TMessage message) => [];

    protected abstract Task HandleAsyncInternal(TMessage message, CancellationToken cancellationToken);

    private static Guid GetMessageId(TMessage message) =>
        message switch
        {
            BaseEvent baseEvent => baseEvent.MessageId,
            BaseCommand baseCommand => baseCommand.MessageId,
            _ => throw new CannotProcessMessageNonTransientException(
                $"{typeof(TMessage).Name} must inherit from {nameof(BaseCommand)} or {nameof(BaseEvent)}.")
        };

    private static string GetMessageType()
    {
        var type = typeof(TMessage);
        return type.FullName ?? type.Name;
    }
}
