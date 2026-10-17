using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class DeliveryZoneReadModelRepository(AppReadDbContext dbContext) : IDeliveryZoneReadModelRepository
{
    public async Task<IReadOnlyList<DeliveryZoneReadModel>> FindCoveringAsync(GeographicCoordinates coordinates, DateTimeOffset effectiveAtUtc, CancellationToken cancellationToken)
    {
        var longitude = (double)coordinates.Longitude;
        var latitude = (double)coordinates.Latitude;
        var rows = await dbContext.Database.SqlQuery<ZoneRow>($"""
            SELECT z."Id", z."Code", z."Name", z."CountryCode", z."Priority"
            FROM deliveries."DeliveryZones" z
            WHERE z."IsDeleted" = FALSE AND z."IsActive" = TRUE
              AND z."EffectiveFromUtc" <= {effectiveAtUtc}
              AND (z."EffectiveToUtc" IS NULL OR z."EffectiveToUtc" > {effectiveAtUtc})
              AND ST_Covers(z."Boundary", ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326))
            ORDER BY z."Priority" DESC, z."Id"
            """).ToListAsync(cancellationToken);
        return rows.Select(x => new DeliveryZoneReadModel(x.Id, x.Code, x.Name, x.CountryCode, x.Priority)).ToArray();
    }

    private sealed class ZoneRow { public Guid Id { get; init; } public string Code { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public string CountryCode { get; init; } = string.Empty; public int Priority { get; init; } }
}
