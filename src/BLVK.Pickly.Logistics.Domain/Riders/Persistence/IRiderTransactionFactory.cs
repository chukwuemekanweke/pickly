namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderTransactionFactory
{
    Task<TResult> ExecuteAsync<TResult>(
        RiderTransactionIsolation isolation,
        RiderTransactionContext context,
        Func<IRiderTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}

public interface IRiderTransaction : IAsyncDisposable
{
    Task FlushAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}

public enum RiderTransactionIsolation
{
    ReadCommitted = 1,
    Serializable = 2
}

public sealed record RiderTransactionContext(string Operation, Guid? ResourceId = null);

public sealed class RiderPersistenceConflictException(string message, Exception innerException)
    : Exception(message, innerException);

public sealed class RiderTransientPersistenceException(
    string message,
    int attempts,
    Exception innerException) : Exception(message, innerException)
{
    public int Attempts { get; } = attempts;
}

public sealed class RiderCommitOutcomeUnknownException(string message, Exception innerException)
    : Exception(message, innerException);
