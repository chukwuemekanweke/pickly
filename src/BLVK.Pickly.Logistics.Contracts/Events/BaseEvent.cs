using Chidelu.Integration.Messaging.RabbitMQ.Core;

namespace BLVK.Pickly.Logistics.Contracts.Events;

public abstract record BaseEvent : IEvent
{
    public DateTimeOffset OccuredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid MessageId { get; set; } = Guid.CreateVersion7();
    public string? FlowId { get; set; }
    public Guid? StakeholderId { get; set; }
    public Guid TenantId { get; set; }
}
