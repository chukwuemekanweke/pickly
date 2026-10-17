using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryQuoteItem : Entity
{
    private DeliveryQuoteItem() { }
    private DeliveryQuoteItem(Guid quoteId, string description, int quantity, decimal weightKg)
    {
        var normalized = description?.Trim() ?? string.Empty;
        if (quoteId == Guid.Empty) throw new ArgumentException("Quote is required.", nameof(quoteId));
        if (normalized.Length is 0 or > 500) throw new ArgumentException("Description is required and must not exceed 500 characters.", nameof(description));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (weightKg <= 0m || decimal.Round(weightKg, 3) != weightKg) throw new ArgumentOutOfRangeException(nameof(weightKg));
        DeliveryQuoteId = quoteId;
        Description = normalized;
        Quantity = quantity;
        WeightKg = weightKg;
    }
    public Guid DeliveryQuoteId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal WeightKg { get; private set; }
    internal static DeliveryQuoteItem Create(Guid quoteId, string description, int quantity, decimal weightKg) => new(quoteId, description, quantity, weightKg);
}
