using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task UpdateDetailsWithoutChangingOwnershipOrDefault()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);

        var result = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(
                created.Location!.Id,
                context.OfficeLocation.Id,
                "Office",
                "Use the side entrance",
                context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(UpdatePickupLocationStatus.Success);
        var updated = context.Repository.Locations.Single();
        updated.Label.ShouldBe("Office");
        updated.LocationId.ShouldBe(context.OfficeLocation.Id);
        updated.AdditionalDirections.ShouldBe("Use the side entrance");
        updated.SenderId.ShouldBe(context.SenderId);
        updated.IsDefault.ShouldBeTrue();
    }
}
