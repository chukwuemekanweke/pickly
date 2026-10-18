namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public enum RiderOperationCode
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3,
    ProfileInactive = 4,
    NotFound = 5,
    InvalidLocation = 6,
    StaleLocation = 7,
    Conflict = 8,
    Expired = 9,
    Forbidden = 10
}
