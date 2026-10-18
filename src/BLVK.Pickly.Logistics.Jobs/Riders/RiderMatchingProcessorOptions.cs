namespace BLVK.Pickly.Logistics.Jobs.Riders;

public sealed class RiderMatchingProcessorOptions
{
    public const string SectionName = "Jobs:Riders:Matching";

    public TimeSpan PollInterval { get; init; }

    public void Validate()
    {
        if (PollInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Rider matching poll interval must be positive.");
        }
    }
}
