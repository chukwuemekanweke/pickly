namespace BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;

public enum UpdateRiderLocationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3,
    ProfileInactive = 4,
    StaleLocation = 5,
    InvalidLocation = 6
}
