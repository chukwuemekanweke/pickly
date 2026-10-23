using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.Operations.ReadModels;

/// <summary>
/// Distinct deliveries that entered <see cref="Status"/> on <see cref="Day"/>, and the distinct riders assigned to them.
/// </summary>
public sealed record DeliveryTransitionDailyCount(
    DateOnly Day,
    DeliveryStatus Status,
    int DeliveryCount,
    int RiderCount);
