using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Operations.GetOperationsSnapshot;

public sealed class When_GettingOperationsSnapshot_WithFutureDate_Should
{
    [Fact]
    public async Task ReturnDateInFutureWithoutQuerying()
    {
        var now = new DateTimeOffset(2026, 10, 23, 23, 59, 0, TimeSpan.Zero);
        var reader = Substitute.For<IOperationsMetricsReader>();
        var sut = new GetOperationsSnapshotHandler(reader, new FixedTimeProvider(now));

        var result = await sut.HandleAsync(new(new DateOnly(2026, 10, 24)), CancellationToken.None);

        result.Status.ShouldBe(GetOperationsSnapshotStatus.DateInFuture);
        result.Snapshot.ShouldBeNull();
        reader.ReceivedCalls().ShouldBeEmpty();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
