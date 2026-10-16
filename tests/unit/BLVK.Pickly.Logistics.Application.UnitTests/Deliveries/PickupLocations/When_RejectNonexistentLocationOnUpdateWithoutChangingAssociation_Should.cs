using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Fact]
    public async Task RejectNonexistentLocationOnUpdateWithoutChangingAssociation()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(context.CreateCommand("Home"), CancellationToken.None);

        var result = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(created.Location!.Id, Guid.CreateVersion7(), "Office", null, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(UpdatePickupLocationStatus.LocationNotFound);
        var saved = context.Repository.Locations.Single();
        saved.LocationId.ShouldBe(context.HomeLocation.Id);
        saved.Label.ShouldBe("Home");
    }
}
