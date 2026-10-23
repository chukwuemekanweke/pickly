using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Operations.Metrics.Snapshot;

public sealed class When_GettingOperationsSnapshot_WithPastDate_Should
{
    [Fact]
    public async Task ReturnOkWithSnapshot()
    {
        var date = new DateOnly(2026, 10, 20);
        var sut = OperationsMetricsControllerFactory.Create(OperationsMetricsControllerFactory.EmptyReader());

        var result = await sut.GetSnapshot(new OperationsSnapshotRequest(date), CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<OperationsSnapshotResponse>().Date.ShouldBe(date);
    }
}
