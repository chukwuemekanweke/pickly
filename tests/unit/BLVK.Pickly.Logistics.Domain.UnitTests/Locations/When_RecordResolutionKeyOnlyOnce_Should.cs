
namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Fact]
    public void RecordResolutionKeyOnlyOnce()
    {
        var location = LocationTestFactory.ProviderResolved();
        var key = LocationTestFactory.ResolutionKey();

        location.AddResolutionKey(key).ShouldBeTrue();
        location.AddResolutionKey(key).ShouldBeFalse();

        location.ResolutionKeys.ShouldHaveSingleItem().Key.ShouldBe(key);
    }
}
