using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class ActiveCurrencyByIdSpecification : Specification<Currency>
{
    public ActiveCurrencyByIdSpecification(Guid currencyId)
    {
        Where(currency => currency.Id == currencyId && currency.IsActive);
    }
}
