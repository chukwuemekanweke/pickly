using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class CurrencyByCodeSpecification : Specification<Currency>
{
    public CurrencyByCodeSpecification(string currencyCode)
    {
        var normalizedCode = currencyCode.Trim().ToUpperInvariant();
        Where(currency => currency.CurrencyCode == normalizedCode);
    }
}
