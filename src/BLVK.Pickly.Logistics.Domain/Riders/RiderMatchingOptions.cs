namespace BLVK.Pickly.Logistics.Domain.Riders;

public sealed class RiderMatchingOptions
{
    public const string SectionName = "Riders:Matching";

    public TimeSpan LocationFreshness { get; init; }
    public TimeSpan MaximumFutureLocationClockSkew { get; init; }
    public int InitialSearchRadiusMeters { get; init; }
    public int MaximumSearchRadiusMeters { get; init; }
    public int SearchRadiusExpansionMeters { get; init; }
    public int MaximumCandidatesPerRound { get; init; }
    public TimeSpan OfferTimeout { get; init; }
    public int MaximumAttemptsPerCycle { get; init; }
    public TimeSpan RetryBackoff { get; init; }
    public int MatchingScanBatchSize { get; init; }
    public int ScheduledDispatchScanBatchSize { get; init; }
    public int MaximumConcurrentAssignmentsPerRider { get; init; }
    public int MaximumTransactionRetryCount { get; init; } = 3;
    public TimeSpan TransactionRetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(25);

    public void Validate()
    {
        if (LocationFreshness <= TimeSpan.Zero || MaximumFutureLocationClockSkew < TimeSpan.Zero ||
            InitialSearchRadiusMeters <= 0 || MaximumSearchRadiusMeters < InitialSearchRadiusMeters ||
            SearchRadiusExpansionMeters < 0 || MaximumCandidatesPerRound <= 0 || OfferTimeout <= TimeSpan.Zero ||
            MaximumAttemptsPerCycle <= 0 || RetryBackoff <= TimeSpan.Zero || MatchingScanBatchSize <= 0 ||
            ScheduledDispatchScanBatchSize <= 0 || MaximumTransactionRetryCount < 0 ||
            TransactionRetryBaseDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Rider matching configuration contains invalid bounds.");
        }

        if (MaximumConcurrentAssignmentsPerRider != 1)
        {
            throw new InvalidOperationException("Phase 6 supports exactly one active delivery per rider.");
        }
    }
}
