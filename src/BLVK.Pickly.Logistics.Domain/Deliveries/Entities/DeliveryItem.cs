using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryItem : Entity
{
    private const int MaxDescriptionLength = 500;

    private DeliveryItem()
    {
    }

    private DeliveryItem(string description, int quantity, decimal weightKg)
    {
        var normalizedDescription = description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedDescription))
        {
            throw new ArgumentException("Parcel description is required.", nameof(description));
        }

        if (normalizedDescription.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Parcel description must not exceed {MaxDescriptionLength} characters.", nameof(description));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Parcel quantity must be greater than zero.");
        }

        if (weightKg <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Parcel weight must be greater than zero.");
        }

        Description = normalizedDescription;
        Quantity = quantity;
        WeightKg = weightKg;
    }

    public Guid DeliveryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal WeightKg { get; private set; }

    public static DeliveryItem Create(string description, int quantity, decimal weightKg) =>
        new(description, quantity, weightKg);

    internal void AttachTo(Guid deliveryId)
    {
        if (deliveryId == Guid.Empty)
        {
            throw new ArgumentException("Delivery is required.", nameof(deliveryId));
        }

        if (DeliveryId != Guid.Empty && DeliveryId != deliveryId)
        {
            throw new InvalidOperationException("Parcel already belongs to another delivery.");
        }

        DeliveryId = deliveryId;
    }
}
