using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics.Snapshot;

public sealed class When_GettingOperationsSnapshot_WithFutureDate_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var reader = Substitute.For<IOperationsMetricsReader>();
        var sut = OperationsMetricsControllerFactory.Create(reader);

        var result = await sut.GetSnapshot(new OperationsSnapshotRequest(new DateOnly(2026, 10, 24)), CancellationToken.None);

        result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
        reader.ReceivedCalls().ShouldBeEmpty();
    }
}
