namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderTransactionFactory
{
    Task<IRiderTransaction> BeginAsync(
        RiderTransactionIsolation isolation,
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

public sealed class RiderPersistenceConflictException(string message, Exception innerException)
    : Exception(message, innerException);
