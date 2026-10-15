using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryByTrackingIdSpecification : Specification<Delivery>
{
    public DeliveryByTrackingIdSpecification(string trackingId)
    {
        var normalizedTrackingId = trackingId?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedTrackingId))
        {
            throw new ArgumentException("Tracking ID is required.", nameof(trackingId));
        }

        Where(delivery => delivery.TrackingId == normalizedTrackingId);
        AddInclude(delivery => delivery.Items);
        AddInclude(delivery => delivery.StatusHistory);
    }
}
