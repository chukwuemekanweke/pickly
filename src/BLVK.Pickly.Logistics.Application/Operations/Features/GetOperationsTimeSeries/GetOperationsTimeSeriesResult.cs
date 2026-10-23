namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;

public sealed record GetOperationsTimeSeriesResult(
    GetOperationsTimeSeriesStatus Status,
    OperationsTimeSeriesResponse? TimeSeries = null);
