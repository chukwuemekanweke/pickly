using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task HideLocationFromDifferentSender()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);

        var crossSender = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(
                created.Location!.Id,
                context.OfficeLocation.Id,
                "Other",
                null,
                context.Actor with { StakeholderId = Guid.CreateVersion7() }),
            CancellationToken.None);
        crossSender.Status.ShouldBe(UpdatePickupLocationStatus.NotFound);
    }
}
