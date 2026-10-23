namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;

/// <param name="Date">UTC calendar day to report; defaults to the current UTC day.</param>
public sealed record GetOperationsSnapshotQuery(DateOnly? Date);
