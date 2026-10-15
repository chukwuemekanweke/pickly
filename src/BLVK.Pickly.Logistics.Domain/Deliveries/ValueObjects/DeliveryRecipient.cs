namespace BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

public sealed class DeliveryRecipient
{
    private const int MaxNameLength = 200;

    private DeliveryRecipient()
    {
    }

    private DeliveryRecipient(string name, string phoneNumber)
    {
        Name = NormalizeName(name);
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
    }

    public string Name { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;

    public static DeliveryRecipient Create(string name, string phoneNumber) => new(name, phoneNumber);

    private static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Recipient name is required.", nameof(name));
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new ArgumentException($"Recipient name must not exceed {MaxNameLength} characters.", nameof(name));
        }

        return normalized;
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var value = phoneNumber?.Trim() ?? string.Empty;
        var normalized = string.Concat(value.Where(character => char.IsDigit(character) || character == '+'));
        if (!normalized.StartsWith('+') || normalized.Count(character => character == '+') != 1)
        {
            throw new ArgumentException("Recipient phone number must use international format.", nameof(phoneNumber));
        }

        var digits = normalized[1..];
        if (digits.Length is < 8 or > 15 || digits[0] == '0' || !digits.All(char.IsDigit))
        {
            throw new ArgumentException("Recipient phone number must be a valid international number.", nameof(phoneNumber));
        }

        return normalized;
    }
}
