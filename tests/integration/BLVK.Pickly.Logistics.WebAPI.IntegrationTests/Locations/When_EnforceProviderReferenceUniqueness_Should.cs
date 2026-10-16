using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task EnforceProviderReferenceUniqueness()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var featureId = $"{_run}-duplicate";
        var first = ProviderLocation($"1 Duplicate Road {_run}", featureId);
        var second = ProviderLocation($"1 Duplicate Rd {_run}", featureId);
        _locationIds.AddRange([first.Id, second.Id]);
        dbContext.Locations.AddRange(first, second);

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }
}
