using BLVK.Pickly.Logistics.Application.Common.Pagination;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.PickupLocations;

public sealed partial class When_ReadingPickupLocations_WithScopedCursor_Should
{
[Fact]
    public void RejectCursorFromAnotherEndpoint()
    {
        var cursor = CursorPagination.Encode(DateTimeOffset.UtcNow, Guid.CreateVersion7());

        Should.Throw<InvalidOperationException>(() =>
            CursorPagination.Decode(cursor, "pickup-locations-v1"));
    }
}
