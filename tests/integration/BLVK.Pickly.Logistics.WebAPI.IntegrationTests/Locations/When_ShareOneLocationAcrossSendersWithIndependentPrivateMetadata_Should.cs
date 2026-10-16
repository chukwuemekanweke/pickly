using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Locations;

public sealed partial class When_ManagingSharedLocations_WithPostgis_Should
{
[Fact]
    public async Task ShareOneLocationAcrossSendersWithIndependentPrivateMetadata()
    {
        var otherSender = await CreateSenderAsync();
        var first = await CreatePickupThroughHandlerAsync(_sender, _poloPark.Id, "Home", "Gate A");
        var second = await CreatePickupThroughHandlerAsync(otherSender, _poloPark.Id, "Shop", "Ask for Ada");

        using (var scope = CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<UpdatePickupLocationHandler>();
            (await handler.HandleAsync(
                new UpdatePickupLocationCommand(first.Location!.Id, _poloPark.Id, "Mum's house", "Blue gate", Actor(_sender)),
                CancellationToken.None)).Status.ShouldBe(UpdatePickupLocationStatus.Success);
        }

        using var verificationScope = CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await dbContext.SavedPickupLocations.AsNoTracking()
            .Where(location => location.LocationId == _poloPark.Id)
            .ToListAsync();
        saved.Count.ShouldBe(2);
        saved.Single(location => location.Id == second.Location!.Id).Label.ShouldBe("Shop");
        saved.Single(location => location.Id == second.Location!.Id).AdditionalDirections.ShouldBe("Ask for Ada");
        saved.Single(location => location.Id == first.Location!.Id).Label.ShouldBe("Mum's house");
        var shared = await dbContext.Locations.AsNoTracking().SingleAsync(location => location.Id == _poloPark.Id);
        shared.Address.DisplayAddress.ShouldBe(_poloPark.Address.DisplayAddress);
        LocationGeocodingProvider.CallCount.ShouldBe(0);
    }
}
