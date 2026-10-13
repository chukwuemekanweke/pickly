using BLVK.Pickly.Logistics.Contracts.Commands;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;

namespace BLVK.Pickly.Logistics.Infrastructure.Messaging;

internal sealed class CommandSender(
    IOutboxWriter outboxWriter,
    ICurrentActor currentActor,
    TimeProvider timeProvider) : ICommandSender
{
    public Task SendAsync<TCommand>(TCommand message, CancellationToken cancellationToken)
        where TCommand : BaseCommand
        => SendInternalAsync(message, timeProvider.GetUtcNow(), cancellationToken);

    public Task ScheduleSendAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken)
        where TCommand : BaseCommand
        => SendInternalAsync(message, deliverAtUtc, cancellationToken);

    private Task SendInternalAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken)
        where TCommand : BaseCommand
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

        return outboxWriter.AddCommandAsync(message, deliverAtUtc, cancellationToken);
    }
}
