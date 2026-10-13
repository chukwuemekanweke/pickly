using BLVK.Pickly.Logistics.Application.Payments.Features.ReconcilePayments;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Jobs.Payments;

public sealed class PaymentReconciliationProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<PaymentReconciliationOptions> options,
    BackgroundServiceReadinessState readinessState,
    TimeProvider timeProvider,
    ILogger<PaymentReconciliationProcessor> logger) : BackgroundService
{
    public const string ServiceName = nameof(PaymentReconciliationProcessor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady(ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PaymentReconciliationService>();
                var now = timeProvider.GetUtcNow();

                await service.HandleAsync(
                    now - options.Value.MaxInitiatedAge,
                    now - options.Value.StaleThreshold,
                    now - options.Value.RecheckDelay,
                    options.Value.BatchSize,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment reconciliation iteration failed.");
            }

            await Task.Delay(options.Value.PollInterval, stoppingToken);
        }
    }
}
