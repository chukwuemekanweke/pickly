using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed partial class When_ConfiguringLocationPersistence_Should
{
    [Fact]
    public void StoreCoordinatesAsGistIndexedWgs84Geography()
    {
        using var dbContext = CreateContext();
        var locationType = dbContext.Model.FindEntityType(typeof(Location))!;

        locationType.GetSchema().ShouldBe("locations");
        var coordinates = locationType.FindProperty(nameof(Location.Coordinates))!;
        coordinates.GetColumnType().ShouldBe("geography(Point,4326)");
        coordinates.IsNullable.ShouldBeFalse();
        locationType.GetIndexes().ShouldContain(index =>
            index.Properties.Single().Name == nameof(Location.Coordinates) && index.GetMethod() == "gist");
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_test;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite())
            .Options);
}
