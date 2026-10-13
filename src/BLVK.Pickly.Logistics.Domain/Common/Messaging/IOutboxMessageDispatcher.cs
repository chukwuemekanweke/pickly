namespace BLVK.Pickly.Logistics.Domain.Common.Messaging;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken);
}
