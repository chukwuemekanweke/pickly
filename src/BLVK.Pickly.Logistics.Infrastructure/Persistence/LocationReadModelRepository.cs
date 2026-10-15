using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class LocationReadModelRepository(AppReadDbContext dbContext) : ILocationReadModelRepository
{
    public Task<LocationReadModel?> GetByIdAsync(Guid locationId, CancellationToken cancellationToken) =>
        Project(dbContext.Locations.AsNoTracking().Where(location => location.Id == locationId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<LocationReadModel>> SearchAsync(
        LocationSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Terms.Count == 0)
        {
            return [];
        }

        // Terms are pre-sanitized to letters/digits; each becomes a prefix match. The 'simple' configuration literal
        // must stay inline so PostgreSQL can use the matching GIN expression index on DisplayAddress.
        var tsQuery = string.Join(" & ", request.Terms.Select(term => $"{term}:*"));
        var query = Discoverable(dbContext.Locations.AsNoTracking())
            .Where(location => EF.Functions.ToTsVector("simple", location.Address.DisplayAddress)
                .Matches(EF.Functions.ToTsQuery("simple", tsQuery)));

        if (request.CountryCode is not null)
        {
            query = query.Where(location => location.Address.CountryCode == request.CountryCode);
        }

        return await Project(query
                .OrderByDescending(location => EF.Functions.ToTsVector("simple", location.Address.DisplayAddress)
                    .Rank(EF.Functions.ToTsQuery("simple", tsQuery)))
                .ThenBy(location => location.Address.DisplayAddress)
                .ThenBy(location => location.Id)
                .Take(request.Limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NearbyLocationReadModel>> FindNearbyAsync(
        NearbyLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var latitude = (double)request.Anchor.Latitude;
        var longitude = (double)request.Anchor.Longitude;
        var radiusMeters = (double)request.RadiusMeters;
        var excludeLocationId = request.ExcludeLocationId ?? Guid.Empty;
        var locationType = request.LocationType?.ToString() ?? string.Empty;
        var resolved = nameof(LocationResolutionStatus.Resolved);
        var discoverable = nameof(LocationVisibility.Public);

        // geography arguments make ST_DWithin/ST_Distance work in metres on the WGS84 spheroid; ST_DWithin is
        // answered from the GiST index on "Coordinates". ST_MakePoint takes longitude first.
        var rows = await dbContext.Database
            .SqlQuery<NearbyLocationRow>(
                $"""
                SELECT l."Id", l."DisplayAddress", l."AddressLine", l."City", l."State", l."CountryCode",
                       l."LocationType", l."ResolutionSource", l."ResolutionStatus", l."Visibility",
                       ST_Y(l."Coordinates"::geometry) AS "Latitude",
                       ST_X(l."Coordinates"::geometry) AS "Longitude",
                       ST_Distance(l."Coordinates", anchor.point) AS "DistanceMeters"
                FROM locations."Locations" AS l
                CROSS JOIN (SELECT ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)::geography AS point) AS anchor
                WHERE l."IsDeleted" = FALSE
                  AND l."ResolutionStatus" = {resolved}
                  AND l."Visibility" = {discoverable}
                  AND l."Id" <> {excludeLocationId}
                  AND ({locationType} = '' OR l."LocationType" = {locationType})
                  AND ST_DWithin(l."Coordinates", anchor.point, {radiusMeters})
                ORDER BY "DistanceMeters", l."Id"
                LIMIT {request.Limit}
                """)
            .ToListAsync(cancellationToken);

        return rows.Select(ToReadModel).ToArray();
    }

    private static NearbyLocationReadModel ToReadModel(NearbyLocationRow row)
    {
        var coordinates = GeographicCoordinates.FromDegrees(row.Latitude, row.Longitude);
        return new NearbyLocationReadModel(
            new LocationReadModel(
                row.Id,
                row.DisplayAddress,
                row.AddressLine,
                row.City,
                row.State,
                row.CountryCode,
                coordinates.Latitude,
                coordinates.Longitude,
                Enum.Parse<LocationType>(row.LocationType),
                Enum.Parse<LocationResolutionSource>(row.ResolutionSource),
                Enum.Parse<LocationResolutionStatus>(row.ResolutionStatus),
                Enum.Parse<LocationVisibility>(row.Visibility)),
            row.DistanceMeters);
    }

    private static IQueryable<Location> Discoverable(IQueryable<Location> query) =>
        query.Where(location =>
            location.Visibility == LocationVisibility.Public &&
            location.ResolutionStatus == LocationResolutionStatus.Resolved);

    private static IQueryable<LocationReadModel> Project(IQueryable<Location> query) =>
        query.Select(location => new LocationReadModel(
            location.Id,
            location.Address.DisplayAddress,
            location.Address.AddressLine,
            location.Address.City,
            location.Address.State,
            location.Address.CountryCode,
            location.Coordinates.Latitude,
            location.Coordinates.Longitude,
            location.LocationType,
            location.ResolutionSource,
            location.ResolutionStatus,
            location.Visibility));

    private sealed class NearbyLocationRow
    {
        public Guid Id { get; init; }
        public string DisplayAddress { get; init; } = string.Empty;
        public string AddressLine { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string CountryCode { get; init; } = string.Empty;
        public string LocationType { get; init; } = string.Empty;
        public string ResolutionSource { get; init; } = string.Empty;
        public string ResolutionStatus { get; init; } = string.Empty;
        public string Visibility { get; init; } = string.Empty;
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double DistanceMeters { get; init; }
    }
}
