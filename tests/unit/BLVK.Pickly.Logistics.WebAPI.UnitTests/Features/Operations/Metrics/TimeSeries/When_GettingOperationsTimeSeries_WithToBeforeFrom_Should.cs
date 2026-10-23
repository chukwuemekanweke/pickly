using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics.TimeSeries;

public sealed class When_GettingOperationsTimeSeries_WithToBeforeFrom_Should
{
    [Fact]
    public async Task ReturnValidationProblem()
    {
        var reader = Substitute.For<IOperationsMetricsReader>();
        var sut = OperationsMetricsControllerFactory.Create(reader);

        var result = await sut.GetTimeSeries(
            new OperationsTimeSeriesRequest(new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 9)),
            CancellationToken.None);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBeOfType<ValidationProblemDetails>().Errors.ShouldContainKey(nameof(OperationsTimeSeriesRequest.To));
        reader.ReceivedCalls().ShouldBeEmpty();
    }
}
