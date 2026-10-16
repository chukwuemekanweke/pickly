using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class SavedPickupLocationReadModelRepository(AppReadDbContext dbContext)
    : ISavedPickupLocationReadModelRepository
{
    public Task<SavedPickupLocationReadModel?> GetByIdAsync(
        Guid senderId,
        Guid locationId,
        CancellationToken cancellationToken) =>
        Project(dbContext.SavedPickupLocations.AsNoTracking()
            .Where(location =>
                location.Id == locationId &&
                location.SenderId == senderId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<SavedPickupLocationReadModel?> GetDefaultAsync(
        Guid senderId,
        CancellationToken cancellationToken) =>
        Project(dbContext.SavedPickupLocations.AsNoTracking()
            .Where(location =>
                location.SenderId == senderId &&
                location.IsDefault))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<SavedPickupLocationsCursorPage> ListAsync(
        SavedPickupLocationsCursorRequest request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SavedPickupLocations.AsNoTracking()
            .Where(location => location.SenderId == request.SenderId);

        if (request.CursorCreatedAtUtc.HasValue && request.CursorLocationId.HasValue)
        {
            var cursorCreatedAtUtc = request.CursorCreatedAtUtc.Value;
            var cursorLocationId = request.CursorLocationId.Value;
            query = query.Where(location =>
                location.CreatedAtUtc < cursorCreatedAtUtc ||
                (location.CreatedAtUtc == cursorCreatedAtUtc && location.Id.CompareTo(cursorLocationId) < 0));
        }

        var locations = await Project(query
                .OrderByDescending(location => location.CreatedAtUtc)
                .ThenByDescending(location => location.Id)
                .Take(request.Limit + 1))
            .ToListAsync(cancellationToken);

        var hasMore = locations.Count > request.Limit;
        if (hasMore)
        {
            locations.RemoveAt(locations.Count - 1);
        }

        return new SavedPickupLocationsCursorPage(locations, hasMore);
    }

    private IQueryable<SavedPickupLocationReadModel> Project(
        IQueryable<Domain.Deliveries.Entities.SavedPickupLocation> query) =>
        query.Join(
            dbContext.Locations.AsNoTracking(),
            saved => saved.LocationId,
            shared => shared.Id,
            (saved, shared) => new SavedPickupLocationReadModel(
                saved.Id,
                shared.Id,
                saved.Label,
                saved.AdditionalDirections,
                saved.IsDefault,
                saved.CreatedAtUtc,
                shared.Address.DisplayAddress,
                shared.Address.AddressLine,
                shared.Address.City,
                shared.Address.State,
                shared.Address.CountryCode,
                shared.LocationType,
                shared.Coordinates.Latitude,
                shared.Coordinates.Longitude));
}
