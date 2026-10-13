using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using NSubstitute;
using Shouldly;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenDispatchingEventOutboxMessage_Should
{
    [Fact]
    public async Task PublishEventThroughRabbitMq()
    {
        var publisher = Substitute.For<IPublisher>();
        var sender = Substitute.For<ISender>();
        var stakeholderId = Guid.CreateVersion7();
        var @event = new UserCreated
        {
            StakeholderId = stakeholderId
        };
        var outboxMessage = OutboxMessage.CreateEvent(
            @event.MessageId,
            typeof(UserCreated).FullName.ShouldNotBeNull(),
            JsonSerializer.Serialize(@event),
            @event.OccuredAt,
            @event.OccuredAt);

        var sut = new RabbitMqOutboxMessageDispatcher(publisher, sender);

        await sut.DispatchAsync(outboxMessage, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message => message.MessageId == @event.MessageId && message.StakeholderId == stakeholderId),
            Arg.Any<CancellationToken>(),
            Arg.Any<IDictionary<string, string>?>());
    }
}

