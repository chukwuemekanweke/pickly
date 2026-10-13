namespace BLVK.Pickly.Logistics.Jobs.Authentication;

public sealed class IpAddressLocationEnrichmentOptions
{
    public const string SectionName = "Authentication:IpAddressLocationEnrichment";

    public int BatchSize { get; init; }
    public int PollIntervalSeconds { get; init; }
    public int LocationRefreshIntervalDays { get; init; }

    public void Validate()
    {
        if (BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), "Batch size must be greater than zero.");
        }

        if (PollIntervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PollIntervalSeconds), "Poll interval seconds must be greater than zero.");
        }

        if (LocationRefreshIntervalDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(LocationRefreshIntervalDays), "Location refresh interval days must be greater than zero.");
        }
    }
}
