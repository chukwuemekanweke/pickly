using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderStatus;

public sealed class GetRiderStatusHandler(
    IRiderStatusReader reader,
    IOptions<RiderMatchingOptions> options,
    TimeProvider timeProvider)
{
    public async Task<GetRiderStatusResult> HandleAsync(
        GetRiderStatusQuery query,
        CancellationToken cancellationToken)
    {
        var result = await reader.GetAsync(
            query.ActorContext,
            timeProvider.GetUtcNow(),
            options.Value,
            cancellationToken);
        return new(result.Code switch
        {
            RiderOperationCode.Success => GetRiderStatusStatus.Success,
            RiderOperationCode.NotAuthenticated => GetRiderStatusStatus.NotAuthenticated,
            RiderOperationCode.NotRider => GetRiderStatusStatus.NotRider,
            _ => GetRiderStatusStatus.Conflict
        }, result.Status);
    }
}
