using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
[Theory]
    [InlineData("N", "Country code")]
    [InlineData("NGA", "Country code")]
    [InlineData("1G", "Country code")]
    public void RejectInvalidCountryCode(string countryCode, string expectedMessageFragment)
    {
        Should.Throw<ArgumentException>(() => LocationAddress.Create("12 Road", "Enugu", "Enugu", countryCode))
            .Message.ShouldContain(expectedMessageFragment);
    }
}
