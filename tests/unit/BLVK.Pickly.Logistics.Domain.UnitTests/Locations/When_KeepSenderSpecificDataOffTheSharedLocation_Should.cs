using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

public sealed partial class When_RegisteringLocation_Should
{
[Fact]
    public void KeepSenderSpecificDataOffTheSharedLocation()
    {
        var propertyNames = typeof(Location).GetProperties().Select(property => property.Name).ToArray();

        propertyNames.ShouldNotContain("SenderId");
        propertyNames.ShouldNotContain("Label");
        propertyNames.ShouldNotContain("AdditionalDirections");
        propertyNames.ShouldNotContain("PhoneNumber");
    }
}
