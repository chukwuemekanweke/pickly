using BLVK.Pickly.Logistics.Contracts.Commands;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using System.Diagnostics;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Messaging;

internal sealed class OutboxWriter(
    IRepository<OutboxMessage> repository,
    ICurrentActor currentActor) : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task AddEventAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent =>
        throw new InvalidOperationException("A delivery time must be supplied by the caller.");

    public Task AddEventAsync<TEvent>(TEvent message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TEvent : BaseEvent =>
        AddEventMessageAsync(message, deliverAtUtc, cancellationToken);

    public Task AddCommandAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand =>
        throw new InvalidOperationException("A delivery time must be supplied by the caller.");

    public Task AddCommandAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand =>
        AddCommandMessageAsync(message, deliverAtUtc, cancellationToken);

    private Task AddEventMessageAsync<TEvent>(
        TEvent message,
        DateTimeOffset deliverAtUtc,
        CancellationToken cancellationToken)
        where TEvent : BaseEvent
    {
        var messageType = message.GetType();
        var typeName = messageType.FullName ?? messageType.Name;
        var payload = JsonSerializer.Serialize(message, messageType, SerializerOptions);
        var outboxMessage = OutboxMessage.CreateEvent(
            message.MessageId,
            typeName,
            payload,
            message.OccuredAt,
            deliverAtUtc,
            currentActor.CorrelationId,
            Activity.Current?.Id);

        return repository.AddAsync(outboxMessage, cancellationToken);
    }

    private Task AddCommandMessageAsync<TCommand>(
        TCommand message,
        DateTimeOffset deliverAtUtc,
        CancellationToken cancellationToken)
        where TCommand : BaseCommand
    {
        var messageType = message.GetType();
        var typeName = messageType.FullName ?? messageType.Name;
        var payload = JsonSerializer.Serialize(message, messageType, SerializerOptions);
        var outboxMessage = OutboxMessage.CreateCommand(
            message.MessageId,
            typeName,
            payload,
            message.RequestedAt,
            deliverAtUtc,
            currentActor.CorrelationId,
            Activity.Current?.Id);

        return repository.AddAsync(outboxMessage, cancellationToken);
    }
}
