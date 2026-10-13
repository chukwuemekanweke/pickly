using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class PaymentProviderConfiguration : Entity, IAggregateRoot
{
    private PaymentProviderConfiguration()
    {
    }

    internal PaymentProviderConfiguration(
        Guid id,
        Guid paymentProviderId,
        Guid currencyId,
        PaymentIntent paymentIntent,
        PaymentMethodType paymentMethodType,
        bool isEnabled)
    {
        Id = id;
        PaymentProviderId = paymentProviderId;
        CurrencyId = currencyId;
        PaymentIntent = paymentIntent;
        PaymentMethodType = paymentMethodType;
        IsEnabled = isEnabled;
    }

    public Guid PaymentProviderId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public PaymentIntent PaymentIntent { get; private set; }
    public PaymentMethodType PaymentMethodType { get; private set; }
    public bool IsEnabled { get; private set; }

    internal void Update(PaymentMethodType paymentMethodType, bool isEnabled)
    {
        PaymentMethodType = paymentMethodType;
        IsEnabled = isEnabled;
    }
}
