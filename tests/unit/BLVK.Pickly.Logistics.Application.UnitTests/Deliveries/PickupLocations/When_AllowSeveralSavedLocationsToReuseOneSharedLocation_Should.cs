using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Fact]
    public async Task AllowSeveralSavedLocationsToReuseOneSharedLocation()
    {
        var context = new PickupLocationHandlerTestContext();
        var handler = context.CreateHandler();

        await handler.HandleAsync(context.CreateCommand("Home"), CancellationToken.None);
        await handler.HandleAsync(context.CreateCommand("Weekend drop", additionalDirections: "Gate B"), CancellationToken.None);

        context.Repository.Locations.Select(location => location.LocationId).Distinct().ShouldHaveSingleItem();
        context.Repository.Locations.Select(location => location.Label).ShouldBe(["Home", "Weekend drop"], ignoreOrder: true);
    }
}
