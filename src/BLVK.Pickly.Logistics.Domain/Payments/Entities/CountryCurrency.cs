using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class CountryCurrency : Entity, IAggregateRoot
{
    private CountryCurrency()
    {
    }

    private CountryCurrency(Guid countryId, Guid currencyId, bool isDefault, bool isActive)
    {
        CountryId = countryId;
        CurrencyId = currencyId;
        IsDefault = isDefault;
        IsActive = isActive;
    }

    public Guid CountryId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }

    public static CountryCurrency Create(
        Guid countryId,
        Guid currencyId,
        bool isDefault,
        bool isActive) =>
        new(countryId, currencyId, isDefault, isActive);

    public void SetState(bool isDefault, bool isActive)
    {
        IsDefault = isDefault;
        IsActive = isActive;
    }
}
