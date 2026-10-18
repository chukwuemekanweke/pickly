namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public enum AcceptRiderOfferStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3,
    Forbidden = 4,
    NotFound = 5,
    Expired = 6,
    Conflict = 7
}
