using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;

public sealed class UpdateRiderLocationHandler(
    IRiderLocationPersistence persistence,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<UpdateRiderLocationResult> HandleAsync(
        UpdateRiderLocationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await persistence.UpdateAsync(
            command.ActorContext,
            command.Latitude,
            command.Longitude,
            command.CapturedAtUtc,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => UpdateRiderLocationStatus.Success,
            RiderOperationCode.NotAuthenticated => UpdateRiderLocationStatus.NotAuthenticated,
            RiderOperationCode.NotRider => UpdateRiderLocationStatus.NotRider,
            RiderOperationCode.ProfileInactive => UpdateRiderLocationStatus.ProfileInactive,
            RiderOperationCode.StaleLocation => UpdateRiderLocationStatus.StaleLocation,
            _ => UpdateRiderLocationStatus.InvalidLocation
        });
    }
}
