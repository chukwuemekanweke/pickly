namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsTimeSeries;

public sealed record OperationsTimeSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<OperationsDailyPointResponse> Days);

/// <param name="CompletedDeliveries">Deliveries completed during the day.</param>
/// <param name="ScheduledDeliveries">Scheduled deliveries whose pickup falls on the day, in any status.</param>
/// <param name="RidersCompletingDeliveries">Distinct riders who completed at least one delivery during the day.</param>
/// <param name="RidersAssignedDeliveries">Distinct riders assigned at least one delivery during the day.</param>
public sealed record OperationsDailyPointResponse(
    DateOnly Date,
    int CompletedDeliveries,
    int ScheduledDeliveries,
    int RidersCompletingDeliveries,
    int RidersAssignedDeliveries);
