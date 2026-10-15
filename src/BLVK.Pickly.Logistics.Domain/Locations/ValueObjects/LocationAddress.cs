using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

public sealed partial class LocationAddress
{
    public const int MaxAddressLineLength = 300;
    public const int MaxLocationNameLength = 100;
    public const int MaxDisplayAddressLength = 600;
    public const int MaxNormalizedAddressLength = 600;
    public const int CountryCodeLength = 2;
    public const int ResolutionKeyLength = 64;

    private const string ResolutionKeyVersion = "v1";

    private LocationAddress()
    {
    }

    private LocationAddress(string addressLine, string city, string state, string countryCode, string? displayAddress)
    {
        AddressLine = NormalizeRequired(addressLine, nameof(addressLine), MaxAddressLineLength);
        City = NormalizeRequired(city, nameof(city), MaxLocationNameLength);
        State = NormalizeRequired(state, nameof(state), MaxLocationNameLength);
        CountryCode = NormalizeCountryCode(countryCode);
        DisplayAddress = NormalizeRequired(
            string.IsNullOrWhiteSpace(displayAddress) ? $"{AddressLine}, {City}, {State}, {CountryCode}" : displayAddress,
            nameof(displayAddress),
            MaxDisplayAddressLength);
        NormalizedAddress = NormalizeForIdentity(AddressLine, City, State, CountryCode);
    }

    public string DisplayAddress { get; private set; } = string.Empty;
    public string NormalizedAddress { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;

    public static LocationAddress Create(
        string addressLine,
        string city,
        string state,
        string countryCode,
        string? displayAddress = null) =>
        new(addressLine, city, state, countryCode, displayAddress);

    /// <summary>
    /// Builds the conservative identity string for a full address. Only case, whitespace and punctuation are
    /// normalized; street-name abbreviations are deliberately not expanded so distinct premises are never merged.
    /// </summary>
    public static string NormalizeForIdentity(string addressLine, string city, string state, string countryCode) =>
        string.Join('|',
            NormalizeIdentityPart(addressLine),
            NormalizeIdentityPart(city),
            NormalizeIdentityPart(state),
            NormalizeCountryCode(countryCode).ToLowerInvariant());

    /// <summary>
    /// Returns a SHA-256 key for a normalized resolution request so coordination records and query aliases never
    /// store the raw customer-entered address.
    /// </summary>
    public static string ComputeResolutionKey(string addressLine, string city, string state, string countryCode)
    {
        var identity = $"{ResolutionKeyVersion}|{NormalizeForIdentity(addressLine, city, state, countryCode)}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static string NormalizeRequired(string? value, string argumentName, int maxLength)
    {
        var normalized = CollapseWhitespace(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", argumentName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }

    private static string NormalizeIdentityPart(string? value) =>
        CollapseWhitespace(PunctuationRegex().Replace(value ?? string.Empty, " "))
            .ToLower(CultureInfo.InvariantCulture);

    private static string CollapseWhitespace(string? value) =>
        WhitespaceRegex().Replace(value?.Trim() ?? string.Empty, " ");

    private static string NormalizeCountryCode(string? countryCode)
    {
        var normalized = CollapseWhitespace(countryCode).ToUpperInvariant();
        if (normalized.Length != CountryCodeLength || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("Country code must be a two-letter ISO code.", nameof(countryCode));
        }

        return normalized;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}\s]+")]
    private static partial Regex PunctuationRegex();
}
