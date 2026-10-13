using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class CountryCurrencyByCountryAndCurrencySpecification : Specification<CountryCurrency>
{
    public CountryCurrencyByCountryAndCurrencySpecification(Guid countryId, Guid currencyId)
    {
        Where(mapping =>
            mapping.CountryId == countryId &&
            mapping.CurrencyId == currencyId &&
            mapping.IsActive);
    }
}
