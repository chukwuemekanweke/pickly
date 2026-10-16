
namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Fact]
    public void BecomeUnusableWhenSoftDeleted()
    {
        var location = LocationTestFactory.Landmark();

        location.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test");

        location.IsUsable.ShouldBeFalse();
    }
}
