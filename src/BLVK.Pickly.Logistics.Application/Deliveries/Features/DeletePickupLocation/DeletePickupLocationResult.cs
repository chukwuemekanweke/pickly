namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;

public sealed record DeletePickupLocationResult(DeletePickupLocationStatus Status);

public enum DeletePickupLocationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotFound = 3
}
