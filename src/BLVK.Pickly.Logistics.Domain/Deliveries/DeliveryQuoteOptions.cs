namespace BLVK.Pickly.Logistics.Domain.Deliveries;

public sealed class DeliveryQuoteOptions
{
    public const string SectionName = "Deliveries:Quotes";
    public TimeSpan ValidityDuration { get; init; } = TimeSpan.FromMinutes(15);
    public void Validate()
    {
        if (ValidityDuration < TimeSpan.FromMinutes(1) || ValidityDuration > TimeSpan.FromHours(24)) throw new InvalidOperationException("Deliveries:Quotes:ValidityDuration must be between one minute and 24 hours.");
    }
}
