using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReadingPickupLocations_WithScopedCursor_Should
{
    [Fact]
    public async Task ReturnOwnedLocationAndDefault()
    {
        var repository = Substitute.For<ISavedPickupLocationReadModelRepository>();
        var tenantId = Guid.CreateVersion7();
        var senderId = Guid.CreateVersion7();
        var location = Location();
        var actor = new ActorContext(senderId, tenantId, "correlation", "flow");
        repository.GetByIdAsync(senderId, location.Id, Arg.Any<CancellationToken>()).Returns(location);
        repository.GetDefaultAsync(senderId, Arg.Any<CancellationToken>()).Returns(location);

        var byId = await new GetPickupLocationHandler(repository).HandleAsync(
            new GetPickupLocationQuery(location.Id, actor),
            CancellationToken.None);
        var defaultResult = await new GetDefaultPickupLocationHandler(repository).HandleAsync(
            new GetDefaultPickupLocationQuery(actor),
            CancellationToken.None);

        byId.Status.ShouldBe(GetPickupLocationStatus.Success);
        byId.Location!.Id.ShouldBe(location.Id);
        byId.Location.LocationId.ShouldBe(location.LocationId);
        byId.Location.AdditionalDirections.ShouldBe("Near the gate");
        byId.Location.Address.DisplayAddress.ShouldBe("36 Garden Avenue, Enugu");
        byId.Location.Address.Latitude.ShouldBe(6.4474m);
        defaultResult.Status.ShouldBe(GetDefaultPickupLocationStatus.Success);
    }

    private static SavedPickupLocationReadModel Location() =>
        new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Home",
            "Near the gate",
            true,
            DateTimeOffset.UtcNow,
            "36 Garden Avenue, Enugu",
            "36 Garden Avenue",
            "Enugu",
            "Enugu",
            "NG",
            LocationType.Address,
            6.4474m,
            7.5139m);
}
