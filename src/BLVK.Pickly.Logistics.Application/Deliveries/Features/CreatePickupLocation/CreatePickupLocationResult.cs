namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;

public sealed record CreatePickupLocationResult(
    CreatePickupLocationStatus Status,
    PickupLocationResponse? Location = null);

public enum CreatePickupLocationStatus
{
    Success = 1,
    NotAuthenticated = 2
}
