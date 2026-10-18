using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public sealed class AcceptRiderOfferHandler(
    IRiderOfferAcceptancePersistence persistence,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<AcceptRiderOfferResult> HandleAsync(
        AcceptRiderOfferCommand command,
        CancellationToken cancellationToken)
    {
        var result = await persistence.AcceptAsync(
            command.ActorContext,
            command.OfferId,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => AcceptRiderOfferStatus.Success,
            RiderOperationCode.NotAuthenticated => AcceptRiderOfferStatus.NotAuthenticated,
            RiderOperationCode.NotRider => AcceptRiderOfferStatus.NotRider,
            RiderOperationCode.Forbidden => AcceptRiderOfferStatus.Forbidden,
            RiderOperationCode.NotFound => AcceptRiderOfferStatus.NotFound,
            RiderOperationCode.Expired => AcceptRiderOfferStatus.Expired,
            _ => AcceptRiderOfferStatus.Conflict
        }, result.Delivery);
    }
}
