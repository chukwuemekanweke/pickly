using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Authentication.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Jobs.Authentication;

public sealed class IpAddressLocationEnrichmentProcessor(
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    IOptions<IpAddressLocationEnrichmentOptions> options,
    BackgroundServiceReadinessState readinessState,
    ILogger<IpAddressLocationEnrichmentProcessor> logger) : BackgroundService
{
    public const string ServiceName = nameof(IpAddressLocationEnrichmentProcessor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady(ServiceName);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));

        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "IP address location enrichment iteration failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var ipAddressRepository = scope.ServiceProvider.GetRequiredService<IRepository<IpAddress>>();
        var resolver = scope.ServiceProvider.GetRequiredService<IIpGeolocationService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var utcNow = timeProvider.GetUtcNow();

        var ipAddresses = await ipAddressRepository.ListAsync(
            new PendingIpAddressLocationEnrichmentSpecification(
                options.Value.BatchSize,
                utcNow.AddDays(-options.Value.LocationRefreshIntervalDays)),
            cancellationToken);

        foreach (var ipAddress in ipAddresses)
        {
            try
            {
                var geolocation = await resolver.GetGeolocationAsync(ipAddress.Value, cancellationToken);
                if (geolocation is not null)
                {
                    ipAddress.ApplyLocationResolution(
                        geolocation.City,
                        geolocation.State,
                        geolocation.Country,
                        utcNow);
                }
                else
                {
                    ipAddress.RecordLocationLookup(utcNow);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to resolve geolocation for IP address {IpAddress}",
                    ipAddress.Value);

                ipAddress.RecordLocationLookup(utcNow);
            }

            ipAddressRepository.Update(ipAddress);
        }

        if (ipAddresses.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
