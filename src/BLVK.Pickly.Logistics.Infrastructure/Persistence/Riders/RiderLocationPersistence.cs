using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderLocationPersistence(AppDbContext dbContext) : IRiderLocationPersistence
{
    public Task<RiderCurrentLocation?> FindAsync(Guid riderProfileId, CancellationToken cancellationToken) =>
        dbContext.RiderCurrentLocations.SingleOrDefaultAsync(
            item => item.RiderProfileId == riderProfileId,
            cancellationToken);

    public void TrackPosition(RiderCurrentLocation location, decimal latitude, decimal longitude, bool isNew)
    {
        if (isNew)
        {
            dbContext.RiderCurrentLocations.Add(location);
        }

        dbContext.Entry(location)
            .Property<Point>(RiderCurrentLocationConfiguration.PositionProperty)
            .CurrentValue = RiderGeographyQueries.CreatePoint(latitude, longitude);
    }
}
