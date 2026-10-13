using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.InitiatePayment;

public sealed class InitiatePaymentHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<Currency> currencyRepository,
    IRepository<CountryCurrency> countryCurrencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentProviderConfiguration> paymentProviderConfigurationRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiatePaymentResult> HandleAsync(InitiatePaymentCommand command, CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to initiate a payment.");

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to resolve stakeholder '{stakeholderId}' for payment initiation.");

        var currency = await currencyRepository.FirstOrDefaultAsync(
            new ActiveCurrencyByIdSpecification(command.CurrencyId),
            cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{command.CurrencyId}' is not active.");

        var supportedCountryCurrency = await countryCurrencyRepository.FirstOrDefaultAsync(
            new CountryCurrencyByCountryAndCurrencySpecification(stakeholder.CountryId, command.CurrencyId),
            cancellationToken);

        if (supportedCountryCurrency is null)
        {
            throw new InvalidOperationException(
                $"Currency '{currency.CurrencyCode}' is not supported for country '{stakeholder.CountryId}'.");
        }

        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByIdSpecification(command.PaymentProviderId),
                cancellationToken)
            ?? throw new InvalidOperationException($"Payment provider '{command.PaymentProviderId}' is not active.");

        _ = await paymentProviderConfigurationRepository.FirstOrDefaultAsync(
                new EnabledPaymentProviderConfigurationSpecification(command.PaymentProviderId, command.CurrencyId, command.PaymentIntent),
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Payment provider '{paymentProvider.ProviderName}' does not support '{currency.CurrencyCode}' for '{command.PaymentIntent}'.");

        var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException(
                $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");

        var merchantReference = $"pay_{Guid.CreateVersion7():N}";

        var paymentTransaction = PaymentTransaction.Create(merchantReference, command.PaymentIntent, paymentProvider.Id, command.Amount, command.CurrencyId, stakeholder.CountryId, stakeholder.AppUserId, stakeholder.Id, stakeholder.TenantId);

        await paymentTransactionRepository.AddAsync(paymentTransaction);

        var initiationResult = await paymentProviderService.InitiatePaymentAsync(
            new PaymentProviderInitiationRequest(
                merchantReference,
                command.Amount,
                currency.CurrencyCode,
                command.PaymentIntent,
                stakeholder.Id,
                stakeholder.TenantId,
                stakeholder.CountryId),
            cancellationToken);

        paymentTransaction.MarkInitiated(
            initiationResult.ProviderReference,
            initiationResult.InstructionFields.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
            initiationResult.ExpiresAtUtc,
            KnownPaymentTransactionChangeReasons.PaymentInitiated);
        paymentTransaction.SetPaymentMethodType(initiationResult.PaymentMethodType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.Initiated,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholder.Id,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.Provider] = paymentProvider.ProviderKey,
                    [Observability.PropertyNames.Payments.PaymentMethod] = paymentTransaction.PaymentMethodType.ToString(),
                    [Observability.PropertyNames.Payments.PaymentIntent] = paymentTransaction.PaymentIntent.ToString(),
                    [Observability.PropertyNames.Payments.MerchantReference] = paymentTransaction.MerchantReference,
                    [Observability.PropertyNames.Payments.ProviderReference] = paymentTransaction.ProviderReference ?? string.Empty,
                    [Observability.PropertyNames.Payments.CurrencyCode] = currency.CurrencyCode
                }));

        return new InitiatePaymentResult(
            paymentTransaction.MerchantReference,
            paymentTransaction.PaymentStatus,
            paymentProvider.Id,
            paymentProvider.ProviderName,
            paymentTransaction.ExpiresAtUtc,
            paymentTransaction.PaymentMethodType,
            initiationResult.InstructionFields);
    }
}


