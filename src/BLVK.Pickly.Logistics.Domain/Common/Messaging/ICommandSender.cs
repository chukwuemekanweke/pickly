using BLVK.Pickly.Logistics.Contracts.Commands;

namespace BLVK.Pickly.Logistics.Domain.Common.Messaging;

public interface ICommandSender
{
    Task SendAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;

    Task ScheduleSendAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;
}
