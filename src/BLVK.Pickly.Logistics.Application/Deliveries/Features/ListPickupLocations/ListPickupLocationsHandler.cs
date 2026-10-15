using BLVK.Pickly.Logistics.Application.Common.Pagination;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;

public sealed class ListPickupLocationsHandler(ISavedPickupLocationReadModelRepository repository)
{
    private const string CursorScope = "pickup-locations-v1";

    public async Task<ListPickupLocationsResult> HandleAsync(
        ListPickupLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var senderId = query.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId.Value == Guid.Empty)
        {
            return new ListPickupLocationsResult(
                ListPickupLocationsStatus.NotAuthenticated,
                [],
                null,
                false);
        }

        var (cursorCreatedAtUtc, cursorLocationId) = CursorPagination.Decode(query.Cursor, CursorScope);
        var page = await repository.ListAsync(
            new SavedPickupLocationsCursorRequest(
                senderId.Value,
                cursorCreatedAtUtc,
                cursorLocationId,
                query.Limit),
            cancellationToken);

        var items = page.Locations.Select(PickupLocationResponse.FromReadModel).ToArray();
        var nextCursor = page.HasMore
            ? CursorPagination.Encode(
                page.Locations[^1].CreatedAtUtc,
                page.Locations[^1].Id,
                CursorScope)
            : null;

        return new ListPickupLocationsResult(
            ListPickupLocationsStatus.Success,
            items,
            nextCursor,
            page.HasMore);
    }
}
