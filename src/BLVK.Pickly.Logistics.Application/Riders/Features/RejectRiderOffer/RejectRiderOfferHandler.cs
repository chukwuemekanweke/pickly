using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;

public sealed class RejectRiderOfferHandler(
    IRiderOfferRejectionPersistence persistence,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<RejectRiderOfferResult> HandleAsync(
        RejectRiderOfferCommand command,
        CancellationToken cancellationToken)
    {
        var result = await persistence.RejectAsync(
            command.ActorContext,
            command.OfferId,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => RejectRiderOfferStatus.Success,
            RiderOperationCode.NotAuthenticated => RejectRiderOfferStatus.NotAuthenticated,
            RiderOperationCode.NotRider => RejectRiderOfferStatus.NotRider,
            RiderOperationCode.Forbidden => RejectRiderOfferStatus.Forbidden,
            RiderOperationCode.NotFound => RejectRiderOfferStatus.NotFound,
            RiderOperationCode.Expired => RejectRiderOfferStatus.Expired,
            _ => RejectRiderOfferStatus.Conflict
        });
    }
}
