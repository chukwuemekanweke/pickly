using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderActiveDelivery;

public sealed class GetRiderActiveDeliveryHandler(IRiderActiveDeliveryReader reader)
{
    public async Task<GetRiderActiveDeliveryResult> HandleAsync(
        GetRiderActiveDeliveryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await reader.GetAsync(query.ActorContext, cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => GetRiderActiveDeliveryStatus.Success,
            RiderOperationCode.NotAuthenticated => GetRiderActiveDeliveryStatus.NotAuthenticated,
            RiderOperationCode.NotRider => GetRiderActiveDeliveryStatus.NotRider,
            RiderOperationCode.NotFound => GetRiderActiveDeliveryStatus.NotFound,
            _ => GetRiderActiveDeliveryStatus.Conflict
        }, result.Delivery);
    }
}
