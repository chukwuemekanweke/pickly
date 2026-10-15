using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
[Fact]
    public void DefaultDisplayAddressFromComponents()
    {
        var address = LocationAddress.Create("12 Ekpulechi Avenue", "Enugu", "Enugu", "NG");

        address.DisplayAddress.ShouldBe("12 Ekpulechi Avenue, Enugu, Enugu, NG");
    }
}
