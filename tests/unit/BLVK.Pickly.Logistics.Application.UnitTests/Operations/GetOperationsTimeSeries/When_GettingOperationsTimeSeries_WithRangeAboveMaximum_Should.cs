using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Operations.GetOperationsTimeSeries;

public sealed class When_GettingOperationsTimeSeries_WithRangeAboveMaximum_Should
{
    [Fact]
    public async Task ReturnInvalidRangeWithoutQuerying()
    {
        var to = new DateOnly(2026, 10, 23);
        var reader = Substitute.For<IOperationsMetricsReader>();
        var sut = new GetOperationsTimeSeriesHandler(
            reader,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 23, 0, 0, 0, TimeSpan.Zero)));

        var result = await sut.HandleAsync(
            new(to.AddDays(-GetOperationsTimeSeriesHandler.MaximumDays), to),
            CancellationToken.None);

        result.Status.ShouldBe(GetOperationsTimeSeriesStatus.InvalidRange);
        result.TimeSeries.ShouldBeNull();
        reader.ReceivedCalls().ShouldBeEmpty();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
