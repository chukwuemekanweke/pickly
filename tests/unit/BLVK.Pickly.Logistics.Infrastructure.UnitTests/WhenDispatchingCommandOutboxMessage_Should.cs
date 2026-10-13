using BLVK.Pickly.Logistics.Contracts.Commands;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using NSubstitute;
using Shouldly;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenDispatchingCommandOutboxMessage_Should
{
    [Fact]
    public async Task SendCommandThroughRabbitMq()
    {
        var sender = Substitute.For<ISender>();
        var publisher = Substitute.For<IPublisher>();
        var command = new TestCommand("welcome-email");
        var outboxMessage = OutboxMessage.CreateCommand(
            command.MessageId,
            typeof(TestCommand).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(command),
            command.RequestedAt,
            command.RequestedAt);

        var sut = new RabbitMqOutboxMessageDispatcher(publisher, sender);

        await sut.DispatchAsync(outboxMessage, CancellationToken.None);

        await sender.Received(1).SendAsync(
            Arg.Is<TestCommand>(message => message.MessageId == command.MessageId && message.Name == command.Name),
            Arg.Any<CancellationToken>(),
            Arg.Any<IDictionary<string, string>?>());
    }

    private sealed record TestCommand(string Name) : BaseCommand;
}

