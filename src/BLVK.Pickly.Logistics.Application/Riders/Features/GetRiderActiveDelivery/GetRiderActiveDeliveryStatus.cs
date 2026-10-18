namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderActiveDelivery;

public enum GetRiderActiveDeliveryStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3,
    NotFound = 4,
    Conflict = 5
}
