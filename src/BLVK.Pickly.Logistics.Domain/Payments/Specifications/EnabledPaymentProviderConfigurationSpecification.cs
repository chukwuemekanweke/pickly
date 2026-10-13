using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class EnabledPaymentProviderConfigurationSpecification : Specification<PaymentProviderConfiguration>
{
    public EnabledPaymentProviderConfigurationSpecification(Guid paymentProviderId, Guid currencyId, PaymentIntent paymentIntent)
    {
        Where(configuration =>
            configuration.PaymentProviderId == paymentProviderId &&
            configuration.CurrencyId == currencyId &&
            configuration.PaymentIntent == paymentIntent &&
            configuration.IsEnabled);
    }
}
