using BLVK.Pickly.Logistics.Application.Common.Pagination;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed class When_ReadingPickupLocations_WithScopedCursor_Should
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
        defaultResult.Status.ShouldBe(GetDefaultPickupLocationStatus.Success);
    }

    [Fact]
    public async Task ReturnStableCursorPage()
    {
        var repository = Substitute.For<ISavedPickupLocationReadModelRepository>();
        var tenantId = Guid.CreateVersion7();
        var senderId = Guid.CreateVersion7();
        var location = Location();
        SavedPickupLocationsCursorRequest? captured = null;
        repository.ListAsync(
                Arg.Do<SavedPickupLocationsCursorRequest>(request => captured = request),
                Arg.Any<CancellationToken>())
            .Returns(new SavedPickupLocationsCursorPage([location], true));

        var result = await new ListPickupLocationsHandler(repository).HandleAsync(
            new ListPickupLocationsQuery(
                1,
                null,
                new ActorContext(senderId, tenantId, "correlation", "flow")),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.SenderId.ShouldBe(senderId);
        captured.Limit.ShouldBe(1);
        result.HasMore.ShouldBeTrue();
        result.NextCursor.ShouldNotBeNull();
        CursorPagination.Decode(result.NextCursor, "pickup-locations-v1").EntityId.ShouldBe(location.Id);
    }

    [Fact]
    public void RejectCursorFromAnotherEndpoint()
    {
        var cursor = CursorPagination.Encode(DateTimeOffset.UtcNow, Guid.CreateVersion7());

        Should.Throw<InvalidOperationException>(() =>
            CursorPagination.Decode(cursor, "pickup-locations-v1"));
    }

    private static SavedPickupLocationReadModel Location() =>
        new(
            Guid.CreateVersion7(),
            "Home",
            "36 Garden Avenue",
            null,
            "Enugu",
            "Enugu",
            "NG",
            6.4474m,
            7.5139m,
            true,
            DateTimeOffset.UtcNow);
}
