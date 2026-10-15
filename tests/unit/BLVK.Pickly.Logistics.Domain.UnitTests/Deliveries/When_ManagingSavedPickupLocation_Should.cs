using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_ManagingSavedPickupLocation_Should
{
    [Fact]
    public void NormalizeValidLocation()
    {
        var location = SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            "  Home  ",
            Address(" 36 Garden Avenue ", " ng "));

        location.Label.ShouldBe("Home");
        location.Address.AddressLine.ShouldBe("36 Garden Avenue");
        location.Address.CountryCode.ShouldBe("NG");
        location.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectBlankLabel(string label)
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            label,
            Address()));
    }

    [Fact]
    public void RejectExcessivelyLongLabel()
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            new string('x', SavedPickupLocation.MaxLabelLength + 1),
            Address()));
    }

    [Fact]
    public void RejectEmptySenderIdentifier()
    {
        Should.Throw<ArgumentException>(() => SavedPickupLocation.Create(
            Guid.Empty,
            "Home",
            Address()));
    }

    [Fact]
    public void PreserveOwnershipAndDefaultStateWhenUpdating()
    {
        var senderId = Guid.CreateVersion7();
        var location = SavedPickupLocation.Create(senderId, "Home", Address(), true);

        location.Update(" Office ", Address("Suite C9"));

        location.SenderId.ShouldBe(senderId);
        location.Label.ShouldBe("Office");
        location.Address.AddressLine.ShouldBe("Suite C9");
        location.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void PreventDeletedLocationFromBecomingDefault()
    {
        var location = SavedPickupLocation.Create(
            Guid.CreateVersion7(),
            "Home",
            Address());
        location.SetDeletedAudit(DateTimeOffset.UtcNow, "test");

        Should.Throw<AggregateStateException>(location.SetAsDefault);
    }

    [Fact]
    public void RejectInvalidCoordinatesThroughSharedAddressModel()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DeliveryAddress.Create(
            "1 Road",
            null,
            "Enugu",
            "Enugu",
            "NG",
            91m,
            7.5m));
    }

    private static DeliveryAddress Address(string line = "1 Road", string countryCode = "NG") =>
        DeliveryAddress.Create(line, "Near gate", "Enugu", "Enugu", countryCode, 6.44m, 7.51m);
}
