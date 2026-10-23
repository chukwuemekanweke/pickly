namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;

public sealed record GetOperationsSnapshotResult(
    GetOperationsSnapshotStatus Status,
    OperationsSnapshotResponse? Snapshot = null);
