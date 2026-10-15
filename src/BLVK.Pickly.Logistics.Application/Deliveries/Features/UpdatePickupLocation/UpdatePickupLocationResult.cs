namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;

public sealed record UpdatePickupLocationResult(UpdatePickupLocationStatus Status);

public enum UpdatePickupLocationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotFound = 3
}
