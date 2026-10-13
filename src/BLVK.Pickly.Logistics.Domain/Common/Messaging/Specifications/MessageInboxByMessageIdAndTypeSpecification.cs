using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Domain.Common.Messaging.Specifications;

public sealed class MessageInboxByMessageIdAndTypeSpecification : Specification<MessageInbox>
{
    public MessageInboxByMessageIdAndTypeSpecification(Guid messageId, string messageType)
    {
        Where(inbox => inbox.MessageId == messageId && inbox.MessageType == messageType);
    }
}
