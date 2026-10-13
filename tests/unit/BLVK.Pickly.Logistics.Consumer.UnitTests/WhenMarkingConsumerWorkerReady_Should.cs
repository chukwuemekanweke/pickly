using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests;

public sealed class WhenMarkingConsumerWorkerReady_Should
{
    [Fact]
    public void UpdateReadinessState()
    {
        var state = new WorkerReadinessState();

        state.MarkReady();

        state.IsReady.ShouldBeTrue();
    }
}

