using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_NormalizingLocationAddress_Should
{
[Fact]
    public void RejectMissingRequiredComponent()
    {
        Should.Throw<ArgumentException>(() => LocationAddress.Create("12 Road", " ", "Enugu", "NG"));
    }
}
