namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDefaultPickupLocation;

public sealed record GetDefaultPickupLocationResult(
    GetDefaultPickupLocationStatus Status,
    PickupLocationResponse? Location = null);

public enum GetDefaultPickupLocationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NoDefault = 3
}
