using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class WalletByStakeholderAndCurrencySpecification : Specification<Wallet>
{
    public WalletByStakeholderAndCurrencySpecification(Guid stakeholderId, Guid currencyId)
    {
        Where(wallet => wallet.StakeholderId == stakeholderId && wallet.CurrencyId == currencyId);
        EnableTracking();
    }
}
