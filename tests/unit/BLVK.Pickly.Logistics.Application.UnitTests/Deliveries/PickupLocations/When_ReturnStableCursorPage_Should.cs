using BLVK.Pickly.Logistics.Application.Common.Pagination;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReadingPickupLocations_WithScopedCursor_Should
{
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
}
