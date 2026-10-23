namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;

/// <param name="From">First UTC day (inclusive); defaults to 13 days before <paramref name="To"/>.</param>
/// <param name="To">Last UTC day (inclusive); defaults to the current UTC day.</param>
public sealed record GetOperationsTimeSeriesQuery(DateOnly? From, DateOnly? To);
