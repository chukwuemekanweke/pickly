namespace BLVK.Pickly.Logistics.Jobs.OutboxProcessing;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "Jobs:OutboxProcessing";
    public const string NotificationChannel = "integration_outbox_changed";

    public int BatchSize { get; set; } = 50;

    public int PollIntervalSeconds { get; set; } = 30;

    public int RetryBaseDelaySeconds { get; set; } = 5;

    public int MaxRetryDelaySeconds { get; set; } = 300;
}
