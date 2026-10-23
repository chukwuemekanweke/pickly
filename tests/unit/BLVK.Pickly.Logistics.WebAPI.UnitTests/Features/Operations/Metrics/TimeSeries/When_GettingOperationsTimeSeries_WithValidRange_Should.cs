using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics.TimeSeries;

public sealed class When_GettingOperationsTimeSeries_WithValidRange_Should
{
    [Fact]
    public async Task ReturnOkWithOnePointPerDay()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 7);
        var sut = OperationsMetricsControllerFactory.Create(OperationsMetricsControllerFactory.EmptyReader());

        var result = await sut.GetTimeSeries(new OperationsTimeSeriesRequest(from, to), CancellationToken.None);

        var series = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<OperationsTimeSeriesResponse>();
        series.Days.Count.ShouldBe(7);
        series.Days[0].Date.ShouldBe(from);
        series.Days[^1].Date.ShouldBe(to);
    }
}
