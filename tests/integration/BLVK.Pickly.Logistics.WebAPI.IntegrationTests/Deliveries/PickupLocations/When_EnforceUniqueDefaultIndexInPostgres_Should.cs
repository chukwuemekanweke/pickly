using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedSender_Should
{
[Fact]
    public async Task EnforceUniqueDefaultIndexInPostgres()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.SavedPickupLocations.AddRange(
            SavedPickupLocation.Create(_stakeholderId, "Home", _home.Id, null, true),
            SavedPickupLocation.Create(_stakeholderId, "Office", _office.Id, null, true));

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }
}
