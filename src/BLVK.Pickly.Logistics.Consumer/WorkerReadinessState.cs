namespace BLVK.Pickly.Logistics.Consumer;

public sealed class WorkerReadinessState
{
    private int _isReady;

    public bool IsReady => _isReady == 1;

    public void MarkReady()
    {
        Interlocked.Exchange(ref _isReady, 1);
    }
}
