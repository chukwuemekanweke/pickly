using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Entities;

public sealed class IpAddress : Entity, IAggregateRoot
{
    private const int MaxValueLength = 45;
    private readonly List<IpAddressLocation> locations = [];

    private IpAddress()
    {
    }

    private IpAddress(string value)
    {
        Value = NormalizeValue(value);
    }

    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset? LocationLookupTimestampUtc { get; private set; }
    public IReadOnlyCollection<IpAddressLocation> Locations => locations;

    public static IpAddress Create(string value) => new(value);

    public void RecordLocationLookup(DateTimeOffset lookedUpAtUtc)
    {
        LocationLookupTimestampUtc = lookedUpAtUtc;
    }

    public IpAddressLocation? GetCurrentLocation() =>
        locations.FirstOrDefault(location => location.IsCurrentLocation);

    public void ApplyLocationResolution(
        string? city,
        string? state,
        string? country,
        DateTimeOffset resolvedAtUtc)
    {
        RecordLocationLookup(resolvedAtUtc);

        var currentLocation = GetCurrentLocation();
        if (currentLocation is null)
        {
            locations.Add(IpAddressLocation.Create(Id, city, state, country, resolvedAtUtc));
            return;
        }

        if (currentLocation.Matches(city, state, country))
        {
            return;
        }

        currentLocation.MarkAsHistorical();
        locations.Add(IpAddressLocation.Create(Id, city, state, country, resolvedAtUtc));
    }

    private static string NormalizeValue(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("IP address is required.", nameof(value));
        }

        if (normalized.Length > MaxValueLength)
        {
            throw new ArgumentException($"IP address must not exceed {MaxValueLength} characters.", nameof(value));
        }

        return normalized;
    }
}
