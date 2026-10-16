using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Fact]
    public void BecomeUnusableAndRejectNewResolutionKeysWhenRetired()
    {
        var location = LocationTestFactory.ProviderResolved();

        location.Retire();

        location.IsUsable.ShouldBeFalse();
        location.ResolutionStatus.ShouldBe(LocationResolutionStatus.Retired);
        Should.Throw<AggregateStateException>(() => location.AddResolutionKey(LocationTestFactory.ResolutionKey()));
    }
}
