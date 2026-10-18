using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;

public sealed record ListRiderOffersResult(
    ListRiderOffersStatus Status,
    IReadOnlyCollection<RiderOfferSnapshot>? Offers = null);
