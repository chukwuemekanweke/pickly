using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;

public sealed class SetRiderAvailabilityHandler(
    IRiderAvailabilityPersistence persistence,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<SetRiderAvailabilityResult> HandleAsync(
        SetRiderAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        var result = await persistence.SetAsync(
            command.ActorContext,
            command.IsAvailable,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => SetRiderAvailabilityStatus.Success,
            RiderOperationCode.NotAuthenticated => SetRiderAvailabilityStatus.NotAuthenticated,
            RiderOperationCode.NotRider => SetRiderAvailabilityStatus.NotRider,
            RiderOperationCode.ProfileInactive => SetRiderAvailabilityStatus.ProfileInactive,
            RiderOperationCode.StaleLocation => SetRiderAvailabilityStatus.StaleLocation,
            _ => SetRiderAvailabilityStatus.Conflict
        }, result.Status);
    }
}
