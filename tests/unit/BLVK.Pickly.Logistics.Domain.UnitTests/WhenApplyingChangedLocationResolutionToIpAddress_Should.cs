using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenApplyingChangedLocationResolutionToIpAddress_Should
{
    [Fact]
    public void ReplaceCurrentLocation()
    {
        var firstLookupAtUtc = new DateTimeOffset(2026, 4, 19, 10, 0, 0, TimeSpan.Zero);
        var secondLookupAtUtc = firstLookupAtUtc.AddDays(7);
        var ipAddress = IpAddress.Create("203.0.113.20");

        ipAddress.ApplyLocationResolution("Lagos", "Lagos", "Nigeria", firstLookupAtUtc);
        ipAddress.ApplyLocationResolution("Abuja", "FCT", "Nigeria", secondLookupAtUtc);

        ipAddress.LocationLookupTimestampUtc.ShouldBe(secondLookupAtUtc);
        ipAddress.Locations.Count.ShouldBe(2);
        ipAddress.Locations.Count(location => location.IsCurrentLocation).ShouldBe(1);

        var currentLocation = ipAddress.GetCurrentLocation();
        currentLocation.ShouldNotBeNull();
        currentLocation.City.ShouldBe("Abuja");
        currentLocation.State.ShouldBe("FCT");
        currentLocation.Country.ShouldBe("Nigeria");
        currentLocation.ResolvedAtUtc.ShouldBe(secondLookupAtUtc);

        var historicalLocation = ipAddress.Locations.Single(location => !location.IsCurrentLocation);
        historicalLocation.City.ShouldBe("Lagos");
        historicalLocation.State.ShouldBe("Lagos");
        historicalLocation.Country.ShouldBe("Nigeria");
        historicalLocation.ResolvedAtUtc.ShouldBe(firstLookupAtUtc);
    }
}

