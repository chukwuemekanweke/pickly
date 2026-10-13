using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Common.Messaging;

public sealed class MessageInbox : Entity, IAggregateRoot
{
    private MessageInbox()
    {
    }

    private MessageInbox(Guid messageId, string messageType, DateTimeOffset receivedAtUtc)
    {
        MessageId = messageId;
        MessageType = NormalizeMessageType(messageType);
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid MessageId { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public static MessageInbox Create(Guid messageId, string messageType, DateTimeOffset receivedAtUtc) =>
        new(messageId, messageType, receivedAtUtc);

    private static string NormalizeMessageType(string messageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);

        return messageType.Trim();
    }
}
