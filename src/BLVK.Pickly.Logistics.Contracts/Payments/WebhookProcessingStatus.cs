namespace BLVK.Pickly.Logistics.Contracts.Payments;

public enum WebhookProcessingStatus
{
    Received = 1,
    Ignored = 2,
    Processed = 3,
    Failed = 4,
    Duplicate = 5
}
