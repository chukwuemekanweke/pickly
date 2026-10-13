using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Payments;

public sealed class CreditWalletHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<Currency> currencyRepository,
    IRepository<Wallet> walletRepository,
    IRepository<WalletTransaction> walletTransactionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<CreditWalletHandler> logger) : BaseMessageHandler<CreditWalletCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(CreditWalletCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("CreditWalletCommand must contain a valid stakeholder id.");
        }

        var existingWalletTransaction = await walletTransactionRepository.FirstOrDefaultAsync(
            new WalletTransactionByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingWalletTransaction is not null)
        {
            return;
        }

        var currency = await currencyRepository.GetByIdAsync(message.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{message.CurrencyId}' was not found.");
        var wallet = await walletRepository.FirstOrDefaultAsync(
            new WalletByStakeholderAndCurrencySpecification(message.StakeholderId.Value, message.CurrencyId),
            cancellationToken);

        if (wallet is null)
        {
            wallet = Wallet.Create(message.StakeholderId.Value, message.TenantId, message.CurrencyId);
            wallet.Credit(message.Amount);
            await walletRepository.AddAsync(wallet, cancellationToken);
            CustomTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WalletCreated,
                ObservabilityEventProperties.Create(
                    CurrentActorAccessor,
                    message.StakeholderId,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString(),
                        [Observability.PropertyNames.Payments.CurrencyCode] = currency.CurrencyCode,
                        [Observability.PropertyNames.Payments.WalletId] = wallet.Id.ToString()
                    }));
        }
        else
        {
            wallet.Credit(message.Amount);
            walletRepository.Update(wallet);
        }

        var walletFundingNarrative = WalletTransactionNarratives.WalletFunding;

        await walletTransactionRepository.AddAsync(
            WalletTransaction.CreateCredit(wallet.Id, message.PaymentTransactionId, message.MerchantReference, message.Amount, message.CurrencyId, WalletTransactionCategory.WalletFunding, walletFundingNarrative.Title, walletFundingNarrative.CreateDescription()),
            cancellationToken);

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WalletCredited,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                    [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString(),
                    [Observability.PropertyNames.Payments.CurrencyCode] = currency.CurrencyCode,
                    [Observability.PropertyNames.Payments.WalletId] = wallet.Id.ToString()
                }));
    }
}


