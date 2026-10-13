namespace BLVK.Pickly.Logistics.DatabaseMigrator.Healthcheck;

public sealed class DatabaseMigrationState
{
    private readonly object _lock = new();

    public DatabaseMigrationStatus Status { get; private set; } = DatabaseMigrationStatus.Pending;
    public Exception? Failure { get; private set; }

    public void MarkRunning()
    {
        lock (_lock)
        {
            Status = DatabaseMigrationStatus.Running;
            Failure = null;
        }
    }

    public void MarkSucceeded()
    {
        lock (_lock)
        {
            Status = DatabaseMigrationStatus.Succeeded;
            Failure = null;
        }
    }

    public void MarkFailed(Exception exception)
    {
        lock (_lock)
        {
            Status = DatabaseMigrationStatus.Failed;
            Failure = exception;
        }
    }
}
