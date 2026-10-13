using BLVK.Pickly.Logistics.Consumer.Payments;
using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments;

internal sealed class PaymentsConsumerTestContext
{
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActorAccessor CurrentActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IRepository<Wallet> WalletRepository { get; } = Substitute.For<IRepository<Wallet>>();
    public IRepository<WalletTransaction> WalletTransactionRepository { get; } = Substitute.For<IRepository<WalletTransaction>>();
    public IRepository<SubscriptionActivation> SubscriptionActivationRepository { get; } = Substitute.For<IRepository<SubscriptionActivation>>();
    public IRepository<MessageInbox> MessageInboxRepository { get; } = Substitute.For<IRepository<MessageInbox>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 14, 0, 0, TimeSpan.Zero));
    public ILogger<SuccessfulPaymentConfirmedHandler> SuccessfulPaymentConfirmedLogger { get; } = Substitute.For<ILogger<SuccessfulPaymentConfirmedHandler>>();
    public ILogger<CreditWalletHandler> CreditWalletLogger { get; } = Substitute.For<ILogger<CreditWalletHandler>>();
    public ILogger<ActivateSubscriptionHandler> ActivateSubscriptionLogger { get; } = Substitute.For<ILogger<ActivateSubscriptionHandler>>();

    public SuccessfulPaymentConfirmedHandler CreateSuccessfulPaymentConfirmedHandler() =>
        new(CustomTelemetryContext, CurrentActorAccessor, MessageContext, CommandSender, UnitOfWork, Clock, MessageInboxRepository, SuccessfulPaymentConfirmedLogger);

    public CreditWalletHandler CreateCreditWalletHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            CurrencyRepository,
            WalletRepository,
            WalletTransactionRepository,
            UnitOfWork,
            Clock,
            MessageInboxRepository,
            CreditWalletLogger);

    public ActivateSubscriptionHandler CreateActivateSubscriptionHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            SubscriptionActivationRepository,
            UnitOfWork,
            Clock,
            MessageInboxRepository,
            ActivateSubscriptionLogger);

    public CreditWalletCommand CreateCreditWalletCommand(decimal amount, Guid currencyId)
    {
        var command = new CreditWalletCommand(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7().ToString("N")
        };

        return command;
    }

    public ActivateSubscriptionCommand CreateActivateSubscriptionCommand(decimal amount, Guid currencyId)
    {
        var command = new ActivateSubscriptionCommand(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7().ToString("N")
        };

        return command;
    }

    public void SetCorrelationId() => MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

    public Currency CreateCurrency(Guid id, string currencyCode)
    {
        var currency = Currency.Create(currencyCode, currencyCode, true);
        typeof(Domain.Common.Entities.Entity)
            .GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
            .SetValue(currency, id);
        return currency;
    }

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

