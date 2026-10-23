namespace BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;

/// <param name="AsOfUtc">
/// Moment the state counts describe: the end of <paramref name="Date"/>, or now when the day is still in progress.
/// </param>
/// <param name="Revenue">Charges of deliveries completed during the day, one entry per currency.</param>
public sealed record OperationsSnapshotResponse(
    DateOnly Date,
    DateTimeOffset AsOfUtc,
    bool IsDayComplete,
    RiderCountsResponse Riders,
    DeliveryCountsResponse Deliveries,
    IReadOnlyList<RevenueAmountResponse> Revenue);

/// <param name="Available">Availability enabled and no active delivery.</param>
/// <param name="Busy">Holding an assigned, picked-up or in-transit delivery.</param>
/// <param name="Unavailable">Offline, paused or not approved, and not busy.</param>
public sealed record RiderCountsResponse(int Available, int Busy, int Unavailable, int Total);

/// <param name="Pending">State at <c>AsOfUtc</c>: awaiting dispatch or rider assignment.</param>
/// <param name="InProgress">State at <c>AsOfUtc</c>: rider assigned, picked up or in transit.</param>
/// <param name="AwaitingPayment">State at <c>AsOfUtc</c>: awaiting prepayment.</param>
/// <param name="Completed">Deliveries completed during the day.</param>
/// <param name="Cancelled">Deliveries cancelled during the day.</param>
/// <param name="Failed">Deliveries failed during the day.</param>
public sealed record DeliveryCountsResponse(
    int Pending,
    int InProgress,
    int AwaitingPayment,
    int Completed,
    int Cancelled,
    int Failed);
