using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
[Fact]
    public void KeepDifferentPremisesOnTheSameStreetDistinct()
    {
        var first = LocationAddress.Create("12 Ekpulechi Avenue", "Enugu", "Enugu", "NG");
        var second = LocationAddress.Create("14 Ekpulechi Avenue", "Enugu", "Enugu", "NG");
        var street = LocationAddress.Create("Ekpulechi Avenue", "Enugu", "Enugu", "NG");

        first.NormalizedAddress.ShouldNotBe(second.NormalizedAddress);
        first.NormalizedAddress.ShouldNotBe(street.NormalizedAddress);
    }
}
