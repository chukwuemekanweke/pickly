using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task RestrictHardDeletionOfReferencedLocation()
    {
        await CreatePickupThroughHandlerAsync(_sender, _northGate.Id, "Office", null);
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = _northGate.Id;

        var exception = await Should.ThrowAsync<PostgresException>(() =>
            dbContext.Locations.Where(location => location.Id == id).ExecuteDeleteAsync());

        exception.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
    }
}
