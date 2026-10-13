using System.Threading.Channels;

namespace BLVK.Pickly.Logistics.Jobs.OutboxProcessing;

public sealed class OutboxProcessingSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropWrite
    });

    public void Pulse() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        var signalTask = _channel.Reader.ReadAsync(cancellationToken).AsTask();
        var delayTask = Task.Delay(delay, cancellationToken);
        var completedTask = await Task.WhenAny(signalTask, delayTask);
        await completedTask;

        while (_channel.Reader.TryRead(out _))
        {
        }
    }
}
