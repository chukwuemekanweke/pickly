using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task UpdateOwnedLocation()
    {
        var context = new PickupLocationsControllerTestContext();
        var location = context.Location(isDefault: true);
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(location);

        var result = await context.CreateController().Update(
            location.Id,
            new UpdatePickupLocationRequest(context.SharedLocation.Id, "Office", "Side entrance"),
            CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        location.Label.ShouldBe("Office");
        location.LocationId.ShouldBe(context.SharedLocation.Id);
        location.AdditionalDirections.ShouldBe("Side entrance");
        location.IsDefault.ShouldBeTrue();
    }
}
