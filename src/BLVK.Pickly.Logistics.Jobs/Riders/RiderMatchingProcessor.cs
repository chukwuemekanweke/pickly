using BLVK.Pickly.Logistics.Application.Riders;
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
                var service = scope.ServiceProvider.GetRequiredService<RiderOperationsService>();
                var result = await service.ProcessMatchingAsync(stoppingToken);
                if (result.ScheduledDispatchesOpened + result.MatchingAttempts + result.OffersExpired > 0)
                {
                    logger.LogInformation(
                        "Rider matching iteration opened {ScheduledDispatchCount} dispatches, expired {ExpiredOfferCount} offers, attempted {MatchingAttemptCount} matches, created {CreatedOfferCount} offers, and found no candidate {NoCandidateCount} times.",
                        result.ScheduledDispatchesOpened,
                        result.OffersExpired,
                        result.MatchingAttempts,
                        result.OffersCreated,
                        result.NoCandidateAttempts);
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
