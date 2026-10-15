using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task ReturnCursorPage()
    {
        var context = new PickupLocationsControllerTestContext();
        var model = ReadModel();
        context.ReadRepository.ListAsync(
                Arg.Any<SavedPickupLocationsCursorRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new SavedPickupLocationsCursorPage([model], false));

        var result = await context.CreateController().List(
            new ListPickupLocationsRequest(),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<ListPickupLocationsResponse>().Items.Single().Id.ShouldBe(model.Id);
    }
}
