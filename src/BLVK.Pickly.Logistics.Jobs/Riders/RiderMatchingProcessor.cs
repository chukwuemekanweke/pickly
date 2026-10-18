using BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Jobs.Riders;

public sealed class RiderMatchingProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RiderMatchingProcessorOptions> options,
    BackgroundServiceReadinessState readinessState,
    ILogger<RiderMatchingProcessor> logger) : BackgroundService
{
    public const string ServiceName = nameof(RiderMatchingProcessor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady(ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessRiderMatchingHandler>();
                var result = await handler.HandleAsync(new(), stoppingToken);
                if (result.ScheduledDispatchesOpened + result.MatchingAttempts + result.OffersExpired + result.FailedOperations > 0)
                {
                    logger.LogInformation(
                        "Rider matching iteration opened {ScheduledDispatchCount} dispatches, expired {ExpiredOfferCount} offers, attempted {MatchingAttemptCount} matches, created {CreatedOfferCount} offers, found no candidate {NoCandidateCount} times, and isolated {FailedOperationCount} failed operations.",
                        result.ScheduledDispatchesOpened,
                        result.OffersExpired,
                        result.MatchingAttempts,
                        result.OffersCreated,
                        result.NoCandidateAttempts,
                        result.FailedOperations);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Rider matching iteration failed.");
            }

            await Task.Delay(options.Value.PollInterval, stoppingToken);
        }
    }
}
