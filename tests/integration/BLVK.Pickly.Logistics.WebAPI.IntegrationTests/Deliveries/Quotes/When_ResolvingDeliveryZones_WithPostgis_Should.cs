using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries.Quotes;

[Collection(nameof(ContainersCollection))]
public sealed class When_ResolvingDeliveryZones_WithPostgis_Should(ContainersFixture fixture) : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private readonly DeliveryZone _zone = DeliveryZone.Create($"TEST-{Guid.CreateVersion7():N}", "Synthetic zone", "NG", 10, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.DeliveryZones.Add(_zone);
        db.Entry(_zone).Property<MultiPolygon>("Boundary").CurrentValue = CreateBoundary();
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.DeliveryZones.IgnoreQueryFilters().Where(x => x.Id == _zone.Id).ExecuteDeleteAsync();
        await DisposeClientAsync();
    }

    [Theory]
    [InlineData(6.45, 7.55, true)]
    [InlineData(6.40, 7.50, true)]
    [InlineData(6.30, 7.50, false)]
    public async Task IncludeInteriorAndBoundaryPoints(double latitude, double longitude, bool expected)
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeliveryZoneReadModelRepository>();
        var matches = await repository.FindCoveringAsync(GeographicCoordinates.FromDegrees(latitude, longitude), new DateTimeOffset(2026, 10, 16, 0, 0, 0, TimeSpan.Zero), CancellationToken.None);
        matches.Any(x => x.Id == _zone.Id).ShouldBe(expected);
    }

    [Fact]
    public async Task PersistMultiPolygonWithWgs84SridAndGistIndex()
    {
        using var scope = CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;
        var columnType = await database.SqlQuery<string>($"""SELECT format_type(a.atttypid, a.atttypmod) AS "Value" FROM pg_attribute a JOIN pg_class c ON c.oid=a.attrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='deliveries' AND c.relname='DeliveryZones' AND a.attname='Boundary'""").SingleAsync();
        var indexes = await database.SqlQuery<string>($"""SELECT indexdef AS "Value" FROM pg_indexes WHERE schemaname='deliveries' AND tablename='DeliveryZones'""").ToListAsync();
        columnType.ShouldBe("geometry(MultiPolygon,4326)");
        indexes.ShouldContain(x => x.Contains("\"IX_DeliveryZones_Boundary\"") && x.Contains("USING gist"));
    }

    private static MultiPolygon CreateBoundary()
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var ring = factory.CreateLinearRing([new(7.50, 6.40), new(7.60, 6.40), new(7.60, 6.50), new(7.50, 6.50), new(7.50, 6.40)]);
        return factory.CreateMultiPolygon([factory.CreatePolygon(ring)]);
    }
}
