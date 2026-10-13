using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class Currency : Entity, IAggregateRoot
{
    private Currency()
    {
    }

    private Currency(string currencyCode, string currencyName, bool isActive)
    {
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        CurrencyName = currencyName.Trim();
        IsActive = isActive;
    }

    public string CurrencyCode { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static Currency Create(
        string currencyCode,
        string currencyName,
        bool isActive) =>
        new(currencyCode, currencyName, isActive);

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
