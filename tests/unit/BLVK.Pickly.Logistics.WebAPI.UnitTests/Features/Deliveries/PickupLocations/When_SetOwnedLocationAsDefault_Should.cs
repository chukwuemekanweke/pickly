using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task SetOwnedLocationAsDefault()
    {
        var context = new PickupLocationsControllerTestContext();
        var target = context.Location("Office");
        var current = context.Location("Home", true);
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(target, current);

        var result = await context.CreateController().SetDefault(target.Id, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        target.IsDefault.ShouldBeTrue();
        current.IsDefault.ShouldBeFalse();
    }
}
