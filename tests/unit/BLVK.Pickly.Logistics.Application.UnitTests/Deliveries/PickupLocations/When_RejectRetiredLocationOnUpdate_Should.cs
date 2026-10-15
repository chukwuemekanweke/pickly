using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Fact]
    public async Task RejectRetiredLocationOnUpdate()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(context.CreateCommand("Home"), CancellationToken.None);
        context.OfficeLocation.Retire();

        var result = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(created.Location!.Id, context.OfficeLocation.Id, "Office", null, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(UpdatePickupLocationStatus.LocationUnusable);
        context.Repository.Locations.Single().LocationId.ShouldBe(context.HomeLocation.Id);
    }
}
