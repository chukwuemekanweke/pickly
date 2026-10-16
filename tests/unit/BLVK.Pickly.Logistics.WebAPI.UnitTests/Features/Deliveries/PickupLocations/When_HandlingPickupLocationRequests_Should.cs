using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
    [Fact]
    public async Task ReturnCreatedLocationForValidPayload()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().Create(
            context.CreateRequest(),
            CancellationToken.None);

        var created = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        created.Value.ShouldBeOfType<PickupLocationResponse>().IsDefault.ShouldBeTrue();
        await context.Repository.Received(1).AddAsync(
            Arg.Any<SavedPickupLocation>(),
            Arg.Any<CancellationToken>());
    }

    private static SavedPickupLocationReadModel ReadModel() =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Home",
            null,
            true,
            DateTimeOffset.UtcNow,
            "36 Garden Avenue, Enugu",
            "36 Garden Avenue",
            "Enugu",
            "Enugu",
            "NG",
            LocationType.Street,
            6.4474m,
            7.5139m);
}
