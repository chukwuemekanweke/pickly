namespace BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;

public enum SetRiderAvailabilityStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3,
    ProfileInactive = 4,
    StaleLocation = 5,
    Conflict = 6,
    TemporarilyUnavailable = 7
}
