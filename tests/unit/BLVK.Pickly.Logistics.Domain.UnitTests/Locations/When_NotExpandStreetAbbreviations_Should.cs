using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
[Fact]
    public void NotExpandStreetAbbreviations()
    {
        LocationAddress.NormalizeForIdentity("12 Ekpulechi Ave", "Enugu", "Enugu", "NG")
            .ShouldNotBe(LocationAddress.NormalizeForIdentity("12 Ekpulechi Avenue", "Enugu", "Enugu", "NG"));
    }
}
