using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;

public sealed class ListRiderOffersHandler(IRiderOfferReader reader, TimeProvider timeProvider)
{
    public async Task<ListRiderOffersResult> HandleAsync(
        ListRiderOffersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await reader.ListAsync(query.ActorContext, timeProvider.GetUtcNow(), cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => ListRiderOffersStatus.Success,
            RiderOperationCode.NotAuthenticated => ListRiderOffersStatus.NotAuthenticated,
            RiderOperationCode.NotRider => ListRiderOffersStatus.NotRider,
            RiderOperationCode.ProfileInactive => ListRiderOffersStatus.ProfileInactive,
            _ => ListRiderOffersStatus.Conflict
        }, result.Offers);
    }
}
