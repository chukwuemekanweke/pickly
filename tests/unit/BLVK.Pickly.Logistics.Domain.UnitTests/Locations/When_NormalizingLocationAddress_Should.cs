using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
    [Fact]
    public void IgnoreCaseWhitespaceAndPunctuation()
    {
        var first = LocationAddress.Create("12, Ekpulechi  Avenue.", "Enugu", "Enugu", "ng");
        var second = LocationAddress.Create("12 ekpulechi avenue", " ENUGU ", "enugu", "NG");

        first.NormalizedAddress.ShouldBe(second.NormalizedAddress);
        first.NormalizedAddress.ShouldBe("12 ekpulechi avenue|enugu|enugu|ng");
        first.CountryCode.ShouldBe("NG");
    }
}
