using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Payments;

public sealed class SuccessfulPaymentConfirmedHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<SuccessfulPaymentConfirmedHandler> logger) : BaseMessageHandler<SuccessfulPaymentConfirmed>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SuccessfulPaymentConfirmed message, CancellationToken cancellationToken)
    {
        switch (message.PaymentIntent)
        {
            case PaymentIntent.WalletTopUp:
                await commandSender.SendAsync(
                    new CreditWalletCommand(
                        message.PaymentTransactionId,
                        message.MerchantReference,
                        message.Amount,
                        message.CurrencyId)
                    {
                        StakeholderId = message.StakeholderId,
                        TenantId = message.TenantId,
                        FlowId = message.FlowId
                    },
                    cancellationToken);
                CustomTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.CreditWallet,
                    ObservabilityEventProperties.Create(
                        CurrentActorAccessor,
                        message.StakeholderId,
                        additionalProperties: new Dictionary<string, string>
                        {
                            [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                            [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                        }));
                break;

            case PaymentIntent.Subscription:
                await commandSender.SendAsync(
                    new ActivateSubscriptionCommand(
                        message.PaymentTransactionId,
                        message.MerchantReference,
                        message.Amount,
                        message.CurrencyId)
                    {
                        StakeholderId = message.StakeholderId,
                        TenantId = message.TenantId,
                        FlowId = message.FlowId
                    },
                    cancellationToken);
                CustomTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.ActivateSubscription,
                    ObservabilityEventProperties.Create(
                        CurrentActorAccessor,
                        message.StakeholderId,
                        additionalProperties: new Dictionary<string, string>
                        {
                            [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                            [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                        }));
                break;

            default:
                throw new CannotProcessMessageNonTransientException(
                    $"Unsupported payment intent '{message.PaymentIntent}'.");
        }

    }
}
