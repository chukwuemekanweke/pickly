using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
[Fact]
    public async Task RejectEmptyLocationIdOnCreate()
    {
        var context = new PickupLocationHandlerTestContext();

        var result = await context.CreateHandler().HandleAsync(
            new CreatePickupLocationCommand(Guid.Empty, "Home", null, false, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(CreatePickupLocationStatus.LocationNotFound);
        await context.SharedLocations.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
