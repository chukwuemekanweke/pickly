using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;

namespace BLVK.Pickly.Logistics.Infrastructure.Messaging;

internal sealed class EventPublisher(
    IOutboxWriter outboxWriter,
    ICurrentActor currentActor,
    TimeProvider timeProvider) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : BaseEvent
        => PublishInternalAsync(message, timeProvider.GetUtcNow(), cancellationToken);

    public Task SchedulePublishAsync<TEvent>(TEvent message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken)
        where TEvent : BaseEvent
        => PublishInternalAsync(message, deliverAtUtc, cancellationToken);

    private Task PublishInternalAsync<TEvent>(TEvent message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken)
        where TEvent : BaseEvent
    {
        if (!message.StakeholderId.HasValue && Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            message.StakeholderId = stakeholderId;
        }

        if (string.IsNullOrWhiteSpace(message.FlowId))
        {
            message.FlowId = currentActor.FlowId;
        }

        if (message.TenantId == Guid.Empty && currentActor.TenantId.HasValue)
        {
            message.TenantId = currentActor.TenantId.Value;
        }

        return outboxWriter.AddEventAsync(message, deliverAtUtc, cancellationToken);
    }
}
