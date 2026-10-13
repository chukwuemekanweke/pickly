using BLVK.Pickly.Logistics.Application.Payments.Features.ActivatePaymentProvider;
using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;
using BLVK.Pickly.Logistics.Application.Payments.Features.InitiatePayment;
using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;
using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments;

internal sealed class PaymentsControllerTestContext
{
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IRepository<CountryCurrency> CountryCurrencyRepository { get; } = Substitute.For<IRepository<CountryCurrency>>();
    public IRepository<PaymentProvider> PaymentProviderRepository { get; } = Substitute.For<IRepository<PaymentProvider>>();
    public IRepository<PaymentProviderConfiguration> PaymentProviderConfigurationRepository { get; } = Substitute.For<IRepository<PaymentProviderConfiguration>>();
    public IRepository<PaymentTransaction> PaymentTransactionRepository { get; } = Substitute.For<IRepository<PaymentTransaction>>();
    public IRepository<PaymentWebhookInbox> PaymentWebhookInboxRepository { get; } = Substitute.For<IRepository<PaymentWebhookInbox>>();
    public IWalletTransactionReadModelRepository WalletTransactionReadModelRepository { get; } = Substitute.For<IWalletTransactionReadModelRepository>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public ICredoWebhookSignatureValidator CredoWebhookSignatureValidator { get; } = Substitute.For<ICredoWebhookSignatureValidator>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 13, 0, 0, TimeSpan.Zero));
    public List<IPaymentProviderService> PaymentProviderServices { get; } = [];

    public InitiatePaymentHandler CreateInitiatePaymentHandler() =>
        new(
            StakeholderRepository,
            CurrencyRepository,
            CountryCurrencyRepository,
            PaymentProviderRepository,
            PaymentProviderConfigurationRepository,
            PaymentTransactionRepository,
            PaymentProviderServices,
            CustomTelemetryContext,
            UnitOfWork);

    public ActivatePaymentProviderHandler CreateActivatePaymentProviderHandler() =>
        new(PaymentProviderRepository, UnitOfWork);

    public GetStakeholderWalletTransactionsHandler CreateGetStakeholderWalletTransactionsHandler() =>
        new(WalletTransactionReadModelRepository);

    public GetStakeholderWalletTopUpTransactionDetailHandler CreateGetStakeholderWalletTopUpTransactionDetailHandler() =>
        new(WalletTransactionReadModelRepository);

    public ProcessCredoWebhookHandler CreateCredoWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CredoWebhookSignatureValidator,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public ProcessSafeHavenAccountCreditWebhookHandler CreateSafeHavenAccountCreditWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public ProcessSafeHavenAccountDebitWebhookHandler CreateSafeHavenAccountDebitWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public ProcessSafeHavenVirtualAccountTransferWebhookHandler CreateSafeHavenVirtualAccountTransferWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public Stakeholder CreateStakeholder(Guid appUserId, Guid tenantId, Guid countryId) =>
        Stakeholder.Create(appUserId, tenantId, countryId, Guid.CreateVersion7(), "Ada", "Lovelace");

    public Currency CreateCurrency(string currencyCode) =>
        Currency.Create(currencyCode, currencyCode, true);

    public CountryCurrency CreateCountryCurrency(Guid countryId, Guid currencyId, bool isDefault = true, bool isActive = true) =>
        CountryCurrency.Create(countryId, currencyId, isDefault, isActive);

    public PaymentProvider CreatePaymentProvider(string providerName, string providerKey, bool isActive = true) =>
        PaymentProvider.Create(providerName, providerKey, isActive);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}