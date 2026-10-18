using BLVK.Pickly.Logistics.Domain.Riders;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Riders;

public sealed class When_ValidatingRiderMatchingOptions_Should
{
    [Fact]
    public void RequireSingleAssignmentCapacityForPhaseSix()
    {
        var options = Valid(2);

        Should.Throw<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void AcceptValidBoundedPolicy() => Should.NotThrow(() => Valid().Validate());

    private static RiderMatchingOptions Valid(int maximumConcurrentAssignmentsPerRider = 1) => new()
    {
        LocationFreshness = TimeSpan.FromMinutes(2),
        MaximumFutureLocationClockSkew = TimeSpan.FromSeconds(30),
        InitialSearchRadiusMeters = 3000,
        MaximumSearchRadiusMeters = 15000,
        SearchRadiusExpansionMeters = 3000,
        MaximumCandidatesPerRound = 20,
        OfferTimeout = TimeSpan.FromSeconds(30),
        MaximumAttemptsPerCycle = 5,
        RetryBackoff = TimeSpan.FromSeconds(30),
        MatchingScanBatchSize = 50,
        ScheduledDispatchScanBatchSize = 50,
        MaximumConcurrentAssignmentsPerRider = maximumConcurrentAssignmentsPerRider
    };
}
