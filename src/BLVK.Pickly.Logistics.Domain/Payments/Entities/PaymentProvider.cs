using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class PaymentProvider : Entity, IAggregateRoot
{
    private readonly List<PaymentProviderConfiguration> _configurations = [];

    private PaymentProvider()
    {
    }

    private PaymentProvider(string providerName, string providerKey, bool isActive)
    {
        ProviderName = providerName.Trim();
        ProviderKey = providerKey.Trim();
        IsActive = isActive;
    }

    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<PaymentProviderConfiguration> Configurations => _configurations;

    public static PaymentProvider Create(
        string providerName,
        string providerKey,
        bool isActive) =>
        new(providerName, providerKey, isActive);

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void SetConfiguration(
        Guid currencyId,
        PaymentIntent paymentIntent,
        PaymentMethodType paymentMethodType,
        bool isEnabled)
    {
        var existingConfiguration = _configurations.SingleOrDefault(configuration =>
            configuration.CurrencyId == currencyId &&
            configuration.PaymentIntent == paymentIntent);

        if (existingConfiguration is null)
        {
            _configurations.Add(new PaymentProviderConfiguration(
                Guid.CreateVersion7(),
                Id,
                currencyId,
                paymentIntent,
                paymentMethodType,
                isEnabled));
            return;
        }

        existingConfiguration.Update(paymentMethodType, isEnabled);
    }
}
