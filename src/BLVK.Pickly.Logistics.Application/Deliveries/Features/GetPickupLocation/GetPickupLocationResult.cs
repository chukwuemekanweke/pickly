namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;

public sealed record GetPickupLocationResult(
    GetPickupLocationStatus Status,
    PickupLocationResponse? Location = null);

public enum GetPickupLocationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotFound = 3
}
