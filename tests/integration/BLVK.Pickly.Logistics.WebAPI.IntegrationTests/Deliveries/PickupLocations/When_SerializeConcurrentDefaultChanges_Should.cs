using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedSender_Should
{
[Fact]
    public async Task SerializeConcurrentDefaultChanges()
    {
        var creations = await Task.WhenAll(
            CreateThroughHandlerAsync("Home", false),
            CreateThroughHandlerAsync("Office", false));

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await dbContext.SavedPickupLocations
                .Where(location => location.SenderId == _stakeholderId)
                .ToListAsync();
            persisted.Count.ShouldBe(2);
            persisted.Count(location => location.IsDefault).ShouldBe(1);

            var entityType = dbContext.Model.FindEntityType(typeof(SavedPickupLocation));
            entityType.ShouldNotBeNull();
            entityType.GetIndexes().ShouldContain(index =>
                index.IsUnique &&
                index.GetFilter() == "\"IsDefault\" = TRUE AND \"IsDeleted\" = FALSE");
        }

        await Task.WhenAll(
            SetDefaultThroughHandlerAsync(creations[0].Location!.Id),
            SetDefaultThroughHandlerAsync(creations[1].Location!.Id));

        using var verificationScope = CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var defaults = await verificationContext.SavedPickupLocations
            .CountAsync(location => location.SenderId == _stakeholderId && location.IsDefault);
        defaults.ShouldBe(1);
    }
}
