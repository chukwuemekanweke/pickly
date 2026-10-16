
namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Theory]
    [InlineData("")]
    [InlineData("not-a-sha256-digest")]
    [InlineData("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789")]
    public void RejectMalformedResolutionKey(string key)
    {
        var location = LocationTestFactory.ProviderResolved();

        Should.Throw<ArgumentException>(() => location.AddResolutionKey(key));
    }
}
