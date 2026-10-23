using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics.TimeSeries;

public sealed class When_GettingOperationsTimeSeries_WithRangeAboveMaximum_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var reader = Substitute.For<IOperationsMetricsReader>();
        var sut = OperationsMetricsControllerFactory.Create(reader);

        var result = await sut.GetTimeSeries(
            new OperationsTimeSeriesRequest(new DateOnly(2024, 1, 1), new DateOnly(2026, 1, 1)),
            CancellationToken.None);

        result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
        reader.ReceivedCalls().ShouldBeEmpty();
    }
}
