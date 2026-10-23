using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics.Snapshot;

public sealed class When_GettingOperationsSnapshot_WithDateBeforeEarliest_Should
{
    [Fact]
    public async Task ReturnValidationProblem()
    {
        var reader = Substitute.For<IOperationsMetricsReader>();
        var sut = OperationsMetricsControllerFactory.Create(reader);

        var result = await sut.GetSnapshot(new OperationsSnapshotRequest(new DateOnly(2019, 12, 31)), CancellationToken.None);

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBeOfType<ValidationProblemDetails>().Errors.ShouldContainKey(nameof(OperationsSnapshotRequest.Date));
        reader.ReceivedCalls().ShouldBeEmpty();
    }
}
