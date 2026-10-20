namespace BLVK.Pickly.Logistics.Domain.Deliveries;

public sealed class DeliveryNotificationOptions
{
    public const string SectionName = "Notifications:Delivery";

    public string ApplicationBaseUrl { get; init; } = string.Empty;
    public string DisplayTimeZoneId { get; init; } = string.Empty;

    public void Validate()
    {
        if (!Uri.TryCreate(ApplicationBaseUrl, UriKind.Absolute, out var applicationUri) ||
            applicationUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(ApplicationBaseUrl)} must be an absolute HTTPS URL.");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(DisplayTimeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(DisplayTimeZoneId)} must identify an installed time zone.",
                exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(DisplayTimeZoneId)} must identify a valid time zone.",
                exception);
        }
    }
}
