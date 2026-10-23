namespace BLVK.Pickly.Logistics.WebAPI.Features.Operations.Metrics;

public sealed record OperationsTimeSeriesRequest(DateOnly? From, DateOnly? To);
