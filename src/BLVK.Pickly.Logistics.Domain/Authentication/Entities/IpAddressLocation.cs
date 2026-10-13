using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Entities;

public sealed class IpAddressLocation : Entity
{
    private const int MaxCityLength = 150;
    private const int MaxStateLength = 150;
    private const int MaxCountryLength = 150;

    private IpAddressLocation()
    {
    }

    private IpAddressLocation(
        Guid ipAddressId,
        string? city,
        string? state,
        string? country,
        DateTimeOffset resolvedAtUtc)
    {
        IpAddressId = ipAddressId;
        City = NormalizeOptional(city, MaxCityLength);
        State = NormalizeOptional(state, MaxStateLength);
        Country = NormalizeOptional(country, MaxCountryLength);
        IsCurrentLocation = true;
        ResolvedAtUtc = resolvedAtUtc;
    }

    public Guid IpAddressId { get; private set; }
    public IpAddress? IpAddress { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public bool IsCurrentLocation { get; private set; }
    public DateTimeOffset ResolvedAtUtc { get; private set; }

    internal static IpAddressLocation Create(
        Guid ipAddressId,
        string? city,
        string? state,
        string? country,
        DateTimeOffset resolvedAtUtc) =>
        new(ipAddressId, city, state, country, resolvedAtUtc);

    internal void MarkAsHistorical()
    {
        IsCurrentLocation = false;
    }

    internal bool Matches(string? city, string? state, string? country) =>
        string.Equals(City, NormalizeOptional(city, MaxCityLength), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(State, NormalizeOptional(state, MaxStateLength), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Country, NormalizeOptional(country, MaxCountryLength), StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", nameof(value));
        }

        return normalized;
    }
}
