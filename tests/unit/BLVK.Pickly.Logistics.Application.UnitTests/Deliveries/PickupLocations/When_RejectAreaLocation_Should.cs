using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReferencingSharedLocation_WithPickupLocationCommands_Should
{
    [Fact]
    public async Task RejectAreaLocationOnCreate()
    {
        var context = new PickupLocationHandlerTestContext();
        SetLocationType(context.HomeLocation, LocationType.Area);

        var result = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Area"),
            CancellationToken.None);

        result.Status.ShouldBe(CreatePickupLocationStatus.LocationUnusable);
        context.Repository.Locations.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejectAreaLocationOnUpdate()
    {
        var context = new PickupLocationHandlerTestContext();
        var created = await context.CreateHandler().HandleAsync(
            context.CreateCommand("Home"),
            CancellationToken.None);
        SetLocationType(context.OfficeLocation, LocationType.Area);

        var result = await context.UpdateHandler().HandleAsync(
            new UpdatePickupLocationCommand(
                created.Location!.Id,
                context.OfficeLocation.Id,
                "Area",
                null,
                context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(UpdatePickupLocationStatus.LocationUnusable);
    }

    private static void SetLocationType(Location location, LocationType locationType) =>
        typeof(Location).GetProperty(nameof(Location.LocationType))!.SetValue(location, locationType);
}
