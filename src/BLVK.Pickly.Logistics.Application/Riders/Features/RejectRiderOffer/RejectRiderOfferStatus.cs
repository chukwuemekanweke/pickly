namespace BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;

public enum RejectRiderOfferStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3,
    Forbidden = 4,
    NotFound = 5,
    Expired = 6,
    Conflict = 7,
    TemporarilyUnavailable = 8
}
