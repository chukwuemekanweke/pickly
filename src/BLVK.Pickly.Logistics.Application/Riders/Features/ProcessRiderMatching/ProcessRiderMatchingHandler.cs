using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;

public sealed class ProcessRiderMatchingHandler(
    IRiderMatchingPersistence persistence,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<ProcessRiderMatchingResult> HandleAsync(
        ProcessRiderMatchingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await persistence.ProcessAsync(timeProvider.GetUtcNow(), options.Value, cancellationToken);
        return new(
            result.ScheduledDispatchesOpened,
            result.OffersExpired,
            result.MatchingAttempts,
            result.OffersCreated,
            result.NoCandidateAttempts);
    }
}
