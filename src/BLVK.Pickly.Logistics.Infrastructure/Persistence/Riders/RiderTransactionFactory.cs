using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderTransactionFactory(AppDbContext dbContext) : IRiderTransactionFactory
{
    public async Task<IRiderTransaction> BeginAsync(
        RiderTransactionIsolation isolation,
        CancellationToken cancellationToken)
    {
        var databaseIsolation = isolation switch
        {
            RiderTransactionIsolation.ReadCommitted => IsolationLevel.ReadCommitted,
            RiderTransactionIsolation.Serializable => IsolationLevel.Serializable,
            _ => throw new ArgumentOutOfRangeException(nameof(isolation))
        };
        var transaction = await dbContext.Database.BeginTransactionAsync(databaseIsolation, cancellationToken);
        return new RiderTransaction(dbContext, transaction);
    }

    private sealed class RiderTransaction(
        AppDbContext dbContext,
        IDbContextTransaction transaction) : IRiderTransaction
    {
        private bool _completed;

        public Task FlushAsync(CancellationToken cancellationToken) =>
            SaveChangesAsync(cancellationToken);

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _completed = true;
            }
            catch (Exception exception) when (IsConflict(exception))
            {
                await RollbackAsync(cancellationToken);
                throw new RiderPersistenceConflictException("The rider operation conflicted with concurrent persisted state.", exception);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                dbContext.ChangeTracker.Clear();
            }

            await transaction.DisposeAsync();
        }

        private async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (IsConflict(exception))
            {
                await RollbackAsync(cancellationToken);
                throw new RiderPersistenceConflictException("The rider operation conflicted with concurrent persisted state.", exception);
            }
        }

        private async Task RollbackAsync(CancellationToken cancellationToken)
        {
            if (!_completed)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                _completed = true;
            }
        }

        private static bool IsConflict(Exception exception) => exception switch
        {
            DbUpdateConcurrencyException => true,
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => true,
            PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } => true,
            _ => false
        };
    }
}
