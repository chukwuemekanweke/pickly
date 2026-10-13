using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Payments;

public sealed class ActivateSubscriptionHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<SubscriptionActivation> subscriptionActivationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ActivateSubscriptionHandler> logger) : BaseMessageHandler<ActivateSubscriptionCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(ActivateSubscriptionCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("ActivateSubscriptionCommand must contain a valid stakeholder id.");
        }

        var existingActivation = await subscriptionActivationRepository.FirstOrDefaultAsync(
            new SubscriptionActivationByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existingActivation is not null)
        {
            return;
        }

        await subscriptionActivationRepository.AddAsync(
            SubscriptionActivation.Create(
                message.PaymentTransactionId,
                message.StakeholderId.Value,
                message.TenantId,
                null,
                message.Amount,
                message.CurrencyId,
                Clock.GetUtcNow()),
            cancellationToken);

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.SubscriptionActivated,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                    [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                }));
    }
}
