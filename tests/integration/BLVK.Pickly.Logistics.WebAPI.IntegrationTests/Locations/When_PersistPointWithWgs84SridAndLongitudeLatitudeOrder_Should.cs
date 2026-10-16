using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task PersistPointWithWgs84SridAndLongitudeLatitudeOrder()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = _poloPark.Id;

        var srid = await dbContext.Database
            .SqlQuery<int>($"""SELECT ST_SRID("Coordinates"::geometry) AS "Value" FROM locations."Locations" WHERE "Id" = {id}""")
            .SingleAsync();
        var longitude = await dbContext.Database
            .SqlQuery<double>($"""SELECT ST_X("Coordinates"::geometry) AS "Value" FROM locations."Locations" WHERE "Id" = {id}""")
            .SingleAsync();
        var latitude = await dbContext.Database
            .SqlQuery<double>($"""SELECT ST_Y("Coordinates"::geometry) AS "Value" FROM locations."Locations" WHERE "Id" = {id}""")
            .SingleAsync();
        var reloaded = await dbContext.Locations.AsNoTracking().SingleAsync(location => location.Id == id);

        srid.ShouldBe(4326);
        longitude.ShouldBe(7.5005, 1e-9);
        latitude.ShouldBe(6.4420, 1e-9);
        reloaded.Coordinates.ShouldBe(GeographicCoordinates.Create(6.4420m, 7.5005m));
    }
}
