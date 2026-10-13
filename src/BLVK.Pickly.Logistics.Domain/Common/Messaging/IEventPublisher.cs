using BLVK.Pickly.Logistics.Contracts.Events;

namespace BLVK.Pickly.Logistics.Domain.Common.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : BaseEvent;

    Task SchedulePublishAsync<TEvent>(TEvent message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken)
        where TEvent : BaseEvent;
}
