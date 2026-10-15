namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;

public sealed record SetDefaultPickupLocationResult(SetDefaultPickupLocationStatus Status);

public enum SetDefaultPickupLocationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotFound = 3
}
