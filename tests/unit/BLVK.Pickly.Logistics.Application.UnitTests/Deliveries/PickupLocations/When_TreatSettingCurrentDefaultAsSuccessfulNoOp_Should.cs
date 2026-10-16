using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedActor_Should
{
[Fact]
    public async Task TreatSettingCurrentDefaultAsSuccessfulNoOp()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Home", false),
            CancellationToken.None);
        var saveCountBefore = context.UnitOfWork.SaveCount;

        var result = await context.SetDefaultHandler().HandleAsync(
            new SetDefaultPickupLocationCommand(created.Location!.Id, context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(SetDefaultPickupLocationStatus.Success);
        context.UnitOfWork.SaveCount.ShouldBe(saveCountBefore);
    }
}
