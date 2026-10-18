using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderGeographyQueries(AppDbContext dbContext)
{
    public async Task<Guid?> FindPickupZoneIdAsync(
        Delivery delivery,
        Point pickupPoint,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var quotedOriginZoneId = await dbContext.DeliveryQuotes
            .Where(quote => quote.Id == delivery.DeliveryQuoteId)
            .Select(quote => (Guid?)quote.OriginZoneId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!quotedOriginZoneId.HasValue)
        {
            return null;
        }

        var isStillSupported = await dbContext.DeliveryZones.AnyAsync(
            zone =>
                zone.Id == quotedOriginZoneId.Value &&
                zone.IsActive &&
                zone.CountryCode == delivery.PickupAddress.CountryCode &&
                zone.EffectiveFromUtc <= utcNow &&
                (zone.EffectiveToUtc == null || zone.EffectiveToUtc > utcNow) &&
                EF.Property<MultiPolygon>(zone, DeliveryZoneConfiguration.BoundaryProperty).Covers(pickupPoint),
            cancellationToken);
        return isStillSupported ? quotedOriginZoneId : null;
    }

    public static Point CreatePoint(decimal latitude, decimal longitude) =>
        new((double)longitude, (double)latitude) { SRID = 4326 };
}
