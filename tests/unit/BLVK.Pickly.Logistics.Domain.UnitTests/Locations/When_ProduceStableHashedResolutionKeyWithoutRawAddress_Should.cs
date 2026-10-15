using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
[Fact]
    public void ProduceStableHashedResolutionKeyWithoutRawAddress()
    {
        var key = LocationAddress.ComputeResolutionKey("12, Ekpulechi Avenue", "Enugu", "Enugu", "NG");

        key.Length.ShouldBe(LocationAddress.ResolutionKeyLength);
        key.ShouldBe(LocationAddress.ComputeResolutionKey("12 ekpulechi avenue", "ENUGU", "Enugu", "ng"));
        key.ShouldNotContain("ekpulechi");
    }
}
