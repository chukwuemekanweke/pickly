using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Locations;

internal static class LocationTestFactory
{
    public static readonly DateTimeOffset ResolvedAt = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    public static LocationAddress Address(string line = "12 Ekpulechi Avenue") =>
        LocationAddress.Create(line, "Enugu", "Enugu", "NG", $"{line}, Thinkers Corner, Enugu");

    public static Location ProviderResolved(
        string line = "12 Ekpulechi Avenue",
        string featureId = "dXJuOm1ieGFkcjox",
        LocationVisibility visibility = LocationVisibility.Private) =>
        Location.RegisterProviderResolved(
            Address(line),
            GeographicCoordinates.Create(6.4512m, 7.5221m),
            visibility,
            LocationResolutionSource.Mapbox,
            "Mapbox",
            featureId,
            "address",
            ResolvedAt);

    public static Location Landmark(string name = "Polo Park Mall") =>
        Location.RegisterLandmark(
            LocationAddress.Create(name, "Enugu", "Enugu", "NG"),
            GeographicCoordinates.Create(6.4420m, 7.5005m),
            ResolvedAt);

    public static string ResolutionKey(string line = "12 Ekpulechi Avenue") =>
        LocationAddress.ComputeResolutionKey(line, "Enugu", "Enugu", "NG");
}
