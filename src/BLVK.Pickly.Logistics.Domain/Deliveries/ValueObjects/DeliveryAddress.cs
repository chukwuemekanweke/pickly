namespace BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

public sealed class DeliveryAddress
{
    private const int MaxAddressLineLength = 300;
    private const int MaxAdditionalInformationLength = 500;
    private const int MaxLocationNameLength = 100;

    private DeliveryAddress()
    {
    }

    private DeliveryAddress(
        string addressLine,
        string? additionalInformation,
        string city,
        string state,
        string countryCode,
        decimal latitude,
        decimal longitude)
    {
        AddressLine = NormalizeRequired(addressLine, nameof(addressLine), MaxAddressLineLength);
        AdditionalInformation = NormalizeOptional(additionalInformation, nameof(additionalInformation), MaxAdditionalInformationLength);
        City = NormalizeRequired(city, nameof(city), MaxLocationNameLength);
        State = NormalizeRequired(state, nameof(state), MaxLocationNameLength);
        CountryCode = NormalizeCountryCode(countryCode);

        if (latitude is < -90m or > 90m)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees.");
        }

        if (longitude is < -180m or > 180m)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public string AddressLine { get; private set; } = string.Empty;
    public string? AdditionalInformation { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }

    public static DeliveryAddress Create(
        string addressLine,
        string? additionalInformation,
        string city,
        string state,
        string countryCode,
        decimal latitude,
        decimal longitude) =>
        new(addressLine, additionalInformation, city, state, countryCode, latitude, longitude);

    private static string NormalizeRequired(string value, string argumentName, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
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

    private static string? NormalizeOptional(string? value, string argumentName, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        var normalized = countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 2 || !normalized.All(char.IsLetter))
        {
            throw new ArgumentException("Country code must be a two-letter ISO code.", nameof(countryCode));
        }

        return normalized;
    }
}
