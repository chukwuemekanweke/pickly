using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Jobs.OutboxProcessing;

public sealed class OutboxMessageProcessor(
    ILogger<OutboxMessageProcessor> logger,
    IServiceScopeFactory serviceScopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxProcessingOptions> options,
    OutboxProcessingSignal signal,
    BackgroundServiceReadinessState readinessState) : BackgroundService
{
    public const string ServiceName = nameof(OutboxMessageProcessor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady(ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextDelay = await ProcessPendingMessagesAsync(stoppingToken);
                await signal.WaitAsync(nextDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox message processing iteration failed.");
                await signal.WaitAsync(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken);
            }
        }
    }

    private async Task<TimeSpan> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();
        var now = timeProvider.GetUtcNow();

        var messages = await repository.ListAsync(
            new PendingOutboxMessagesSpecification(options.Value.BatchSize, now),
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);
                message.MarkSent(now);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to dispatch outbox message {MessageId} of type {MessageType}",
                    message.MessageId,
                    message.Type);

                message.MarkFailed(now, GetNextRetryAtUtc(message, now), exception.Message);
            }

            repository.Update(message);
        }

        if (messages.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var nextPendingMessage = await repository.FirstOrDefaultAsync(new NextPendingOutboxMessageSpecification(), cancellationToken);
        return GetNextDelay(nextPendingMessage?.AvailableAtUtc, now);
    }

    private DateTimeOffset GetNextRetryAtUtc(OutboxMessage message, DateTimeOffset utcNow)
    {
        var attemptNumber = message.AttemptCount + 1;
        var exponent = Math.Min(attemptNumber - 1, 16);
        var scaledDelay = options.Value.RetryBaseDelaySeconds * Math.Pow(2, exponent);
        var boundedDelay = Math.Min(scaledDelay, options.Value.MaxRetryDelaySeconds);
        return utcNow.AddSeconds(boundedDelay);
    }

    private TimeSpan GetNextDelay(DateTimeOffset? nextAvailableAtUtc, DateTimeOffset utcNow)
    {
        var fallbackDelay = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);

        if (!nextAvailableAtUtc.HasValue)
        {
            return fallbackDelay;
        }

        var nextDelay = nextAvailableAtUtc.Value - utcNow;
        if (nextDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return nextDelay < fallbackDelay
            ? nextDelay
            : fallbackDelay;
    }

    private sealed class PendingOutboxMessagesSpecification : Specification<OutboxMessage>
    {
        public PendingOutboxMessagesSpecification(int batchSize, DateTimeOffset utcNow)
        {
            Where(message => message.SentAtUtc == null && message.AvailableAtUtc <= utcNow);
            ApplyOrderBy(message => message.AvailableAtUtc);
            ApplyThenBy(message => message.EnqueuedAtUtc);
            ApplyPaging(0, batchSize);
            EnableTracking();
        }
    }

    private sealed class NextPendingOutboxMessageSpecification : Specification<OutboxMessage>
    {
        public NextPendingOutboxMessageSpecification()
        {
            Where(message => message.SentAtUtc == null);
            ApplyOrderBy(message => message.AvailableAtUtc);
            ApplyThenBy(message => message.EnqueuedAtUtc);
            ApplyPaging(0, 1);
        }
    }
}
