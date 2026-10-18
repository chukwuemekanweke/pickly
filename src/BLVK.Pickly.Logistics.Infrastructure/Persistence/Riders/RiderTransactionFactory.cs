using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderTransactionFactory(
    AppDbContext dbContext,
    IOptions<RiderMatchingOptions> options,
    ILogger<RiderTransactionFactory> logger) : IRiderTransactionFactory
{
    private static readonly HashSet<string> RiderIntegrityConstraints =
    [
        "IX_Deliveries_RiderId",
        "IX_DeliveryMatchingSessions_DeliveryId",
        "IX_RiderDeliveryOffers_DeliveryId",
        "IX_RiderDeliveryOffers_RiderProfileId"
    ];

    public async Task<TResult> ExecuteAsync<TResult>(
        RiderTransactionIsolation isolation,
        RiderTransactionContext context,
        Func<IRiderTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var maximumAttempts = options.Value.MaximumTransactionRetryCount + 1;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            dbContext.ChangeTracker.Clear();

            try
            {
                await using var transaction = await BeginAsync(isolation, context, cancellationToken);
                return await operation(transaction, cancellationToken);
            }
            catch (Exception exception) when (TryCreateIntegrityConflict(exception, out var conflict))
            {
                throw conflict;
            }
            catch (Exception exception) when (IsTransient(exception))
            {
                if (attempt == maximumAttempts)
                {
                    logger.LogError(
                        exception,
                        "Rider transaction {Operation} for resource {ResourceId} exhausted {AttemptCount} attempts.",
                        context.Operation,
                        context.ResourceId,
                        attempt);
                    throw new RiderTransientPersistenceException(
                        $"Rider transaction '{context.Operation}' exhausted its retry limit.",
                        attempt,
                        exception);
                }

                var delay = RetryDelay(attempt, options.Value.TransactionRetryBaseDelay);
                logger.LogWarning(
                    exception,
                    "Retrying rider transaction {Operation} for resource {ResourceId} after transient database conflict on attempt {AttemptNumber} of {MaximumAttempts}; delay {RetryDelayMs} ms.",
                    context.Operation,
                    context.ResourceId,
                    attempt,
                    maximumAttempts,
                    delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new UnreachableException();
    }

    private async Task<RiderTransaction> BeginAsync(
        RiderTransactionIsolation isolation,
        RiderTransactionContext context,
        CancellationToken cancellationToken)
    {
        var databaseIsolation = isolation switch
        {
            RiderTransactionIsolation.ReadCommitted => IsolationLevel.ReadCommitted,
            RiderTransactionIsolation.Serializable => IsolationLevel.Serializable,
            _ => throw new ArgumentOutOfRangeException(nameof(isolation))
        };
        var transaction = await dbContext.Database.BeginTransactionAsync(databaseIsolation, cancellationToken);
        return new RiderTransaction(dbContext, transaction, context, logger);
    }

    private static TimeSpan RetryDelay(int attempt, TimeSpan baseDelay)
    {
        if (baseDelay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var exponentialMilliseconds = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitterMilliseconds = Random.Shared.NextDouble() * baseDelay.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(Math.Min(exponentialMilliseconds + jitterMilliseconds, 2000));
    }

    private static bool IsTransient(Exception exception)
    {
        return FindPostgresException(exception) is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
        };
    }

    private static bool TryCreateIntegrityConflict(
        Exception exception,
        out RiderPersistenceConflictException conflict)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            conflict = new RiderPersistenceConflictException(
                "The rider operation conflicted with concurrently updated persisted state.",
                exception);
            return true;
        }

        if (FindPostgresException(exception) is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: { } constraintName
            } postgresException && RiderIntegrityConstraints.Contains(constraintName))
        {
            conflict = new RiderPersistenceConflictException(
                $"The rider operation violated concurrency constraint '{constraintName}'.",
                postgresException);
            return true;
        }

        conflict = null!;
        return false;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }

    private sealed class RiderTransaction(
        AppDbContext dbContext,
        IDbContextTransaction transaction,
        RiderTransactionContext context,
        ILogger logger) : IRiderTransaction
    {
        private bool _completed;

        public Task FlushAsync(CancellationToken cancellationToken) =>
            dbContext.SaveChangesAsync(cancellationToken);

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            try
            {
                await transaction.CommitAsync(cancellationToken);
                _completed = true;
            }
            catch (NpgsqlException exception) when (
                exception is not PostgresException
                {
                    SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
                } && exception.IsTransient)
            {
                throw new RiderCommitOutcomeUnknownException(
                    $"The outcome of rider transaction '{context.Operation}' could not be confirmed.",
                    exception);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(
                        exception,
                        "Rollback failed for rider transaction {Operation} on resource {ResourceId}.",
                        context.Operation,
                        context.ResourceId);
                }
            }

            dbContext.ChangeTracker.Clear();
            try
            {
                await transaction.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Disposal failed for rider transaction {Operation} on resource {ResourceId}.",
                    context.Operation,
                    context.ResourceId);
            }
        }
    }
}
