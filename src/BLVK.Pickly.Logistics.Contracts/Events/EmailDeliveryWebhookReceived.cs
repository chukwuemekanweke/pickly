namespace BLVK.Pickly.Logistics.Contracts.Events;

public sealed record EmailDeliveryWebhookReceived : BaseEvent
{
    public Guid ProviderId { get; init; }
    public string EventId { get; init; } = string.Empty;
    public string ProviderMessageId { get; init; } = string.Empty;
}
