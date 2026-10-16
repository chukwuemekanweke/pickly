using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Fact]
    public async Task KeepSharedLocationUnchangedWhenSenderUpdatesMetadata()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(context.CreateCommand("Home"), CancellationToken.None);
        var displayAddressBefore = context.HomeLocation.Address.DisplayAddress;

        await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(created.Location!.Id, context.HomeLocation.Id, "Mum's place", "Blue gate", context.Actor),
            CancellationToken.None);

        context.HomeLocation.Address.DisplayAddress.ShouldBe(displayAddressBefore);
        context.Repository.Locations.Single().Label.ShouldBe("Mum's place");
    }
}
