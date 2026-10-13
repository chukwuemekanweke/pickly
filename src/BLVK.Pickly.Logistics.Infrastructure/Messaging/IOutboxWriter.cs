using BLVK.Pickly.Logistics.Contracts.Commands;
using BLVK.Pickly.Logistics.Contracts.Events;

namespace BLVK.Pickly.Logistics.Infrastructure.Messaging;

internal interface IOutboxWriter
{
    Task AddEventAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent;

    Task AddEventAsync<TEvent>(TEvent message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent;

    Task AddCommandAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;

    Task AddCommandAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;
}
