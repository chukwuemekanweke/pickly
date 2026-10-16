using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;

public sealed class GetLocationHandler(ILocationReadModelRepository repository)
{
    public async Task<GetLocationResult> HandleAsync(
        GetLocationQuery query,
        CancellationToken cancellationToken)
    {
        var stakeholderId = query.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
        {
            return new GetLocationResult(GetLocationStatus.NotAuthenticated);
        }

        var location = await repository.GetByIdAsync(query.LocationId, cancellationToken);
        return location is null || location.ResolutionStatus != LocationResolutionStatus.Resolved
            ? new GetLocationResult(GetLocationStatus.NotFound)
            : new GetLocationResult(GetLocationStatus.Success, LocationResponse.FromReadModel(location));
    }
}
