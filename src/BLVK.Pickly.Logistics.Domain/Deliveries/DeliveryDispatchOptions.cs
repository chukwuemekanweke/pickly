namespace BLVK.Pickly.Logistics.Domain.Deliveries;

public sealed class DeliveryDispatchOptions
{
    public const string SectionName = "Deliveries:Dispatch";

    public TimeSpan LeadTime { get; init; }

    public void Validate()
    {
        if (LeadTime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Deliveries:Dispatch:LeadTime must be greater than zero.");
        }
    }
}
