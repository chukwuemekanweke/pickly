namespace BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

public sealed class DeliveryCharge
{
    private DeliveryCharge()
    {
    }

    private DeliveryCharge(decimal amount, Guid currencyId)
    {
        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Delivery charge cannot be negative.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Delivery charge cannot have more than two decimal places.", nameof(amount));
        }

        if (currencyId == Guid.Empty)
        {
            throw new ArgumentException("Currency is required.", nameof(currencyId));
        }

        Amount = amount;
        CurrencyId = currencyId;
    }

    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }

    public static DeliveryCharge Create(decimal amount, Guid currencyId) => new(amount, currencyId);
}
