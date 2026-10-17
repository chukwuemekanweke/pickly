namespace BLVK.Pickly.Logistics.Domain.Deliveries;

public sealed class DeliveryDispatchOptions
{
    public const string SectionName = "Deliveries:Dispatch";

    public TimeSpan LeadTime { get; set; } = TimeSpan.FromMinutes(30);

    public void Validate()
    {
        if (LeadTime < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Delivery dispatch lead time cannot be negative.");
        }
    }
}
