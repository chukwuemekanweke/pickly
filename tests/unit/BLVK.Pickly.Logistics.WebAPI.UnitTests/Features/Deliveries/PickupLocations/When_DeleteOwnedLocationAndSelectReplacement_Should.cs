using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task DeleteOwnedLocationAndSelectReplacement()
    {
        var context = new PickupLocationsControllerTestContext();
        var target = context.Location("Home", true);
        var replacement = context.Location("Office");
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(target, replacement);

        var result = await context.CreateController().Delete(target.Id, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        target.IsDeleted.ShouldBeTrue();
        replacement.IsDefault.ShouldBeTrue();
    }
}
