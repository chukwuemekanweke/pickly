using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BLVK.Pickly.Logistics.Consumer.Payments;

public sealed class ConfirmDeliveryPaymentHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<DeliveryPaymentAttempt> attemptRepository,
    IRepository<PaymentTransaction> paymentRepository,
    IRepository<Delivery> deliveryRepository,
    IRepository<Currency> currencyRepository,
    IStakeholderReadModelRepository stakeholders,
    ICommandSender commandSender,
    IOptions<DeliveryNotificationOptions> notificationOptions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ConfirmDeliveryPaymentHandler> logger)
    : BaseMessageHandler<ConfirmDeliveryPaymentCommand>(
        customTelemetryContext,
        currentActorAccessor,
        messageContext,
        messageInboxRepository,
        unitOfWork,
        timeProvider,
        logger)
{
    protected override async Task HandleAsyncInternal(
        ConfirmDeliveryPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var attempt = await attemptRepository.FirstOrDefaultAsync(
            new DeliveryPaymentAttemptByTransactionSpecification(command.PaymentTransactionId, enableTracking: true),
            cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Delivery payment attempt for transaction '{command.PaymentTransactionId}' was not found.");
        var payment = await paymentRepository.GetByIdAsync(command.PaymentTransactionId, cancellationToken);
        if (payment is null ||
            payment.PaymentIntent != PaymentIntent.DeliveryPayment ||
            payment.PaymentStatus != PaymentStatus.Succeeded ||
            payment.VerifiedAmount is null ||
            payment.VerifiedCurrencyId is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Payment transaction '{command.PaymentTransactionId}' is not a verified delivery payment.");
        }

        if (payment.MerchantReference != command.MerchantReference ||
            payment.StakeholderId != command.StakeholderId ||
            payment.TenantId != command.TenantId ||
            payment.VerifiedAmount != command.Amount ||
            payment.VerifiedCurrencyId != command.CurrencyId)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Delivery payment command '{command.MessageId}' does not match persisted provider evidence.");
        }

        var delivery = await deliveryRepository.GetByIdAsync(attempt.DeliveryId, cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Delivery '{attempt.DeliveryId}' was not found for payment '{payment.Id}'.");
        if (delivery.SenderId != payment.StakeholderId ||
            delivery.Charge.Amount != payment.VerifiedAmount ||
            delivery.Charge.CurrencyId != payment.VerifiedCurrencyId)
        {
            attempt.RecordApplicationException("Delivery ownership or agreed charge does not match the verified settlement.");
            return;
        }

        if (delivery.Status == DeliveryStatus.Cancelled)
        {
            attempt.RecordApplicationException("Verified payment arrived after delivery cancellation.");
            return;
        }

        if (delivery.PaymentStatus == DeliveryPaymentStatus.Paid && delivery.PaymentTransactionId != payment.Id)
        {
            attempt.RecordApplicationException("Another successful transaction already satisfied this delivery.");
            return;
        }

        if (attempt.AppliedAtUtc is not null &&
            delivery.PaymentStatus == DeliveryPaymentStatus.Paid &&
            delivery.PaymentTransactionId == payment.Id)
        {
            return;
        }

        delivery.ConfirmVerifiedPrepaidPayment(
            payment.Id,
            payment.VerifiedAmount.Value,
            payment.VerifiedCurrencyId.Value,
            payment.ProviderVerifiedAtUtc ?? Clock.GetUtcNow(),
            ActorAccessor.ActorId,
            "verified_delivery_payment");
        attempt.RecordApplied(Clock.GetUtcNow());
        var currency = await currencyRepository.GetByIdAsync(payment.VerifiedCurrencyId.Value, cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Verified currency '{payment.VerifiedCurrencyId}' was not found.");
        var payer = await stakeholders.GetByStakeholderIdAsync(payment.StakeholderId, cancellationToken)
            ?? throw new CannotProcessMessageNonTransientException(
                $"Paying stakeholder '{payment.StakeholderId}' was not found.");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(notificationOptions.Value.DisplayTimeZoneId);
        var paymentDate = TimeZoneInfo.ConvertTime(
            payment.ProviderVerifiedAtUtc ?? payment.CompletedAtUtc ?? Clock.GetUtcNow(),
            timeZone);
        var actionUrl = new Uri(
            new Uri(notificationOptions.Value.ApplicationBaseUrl.TrimEnd('/') + "/"),
            $"deliveries/{Uri.EscapeDataString(delivery.TrackingId)}").AbsoluteUri;
        await commandSender.SendAsync(
            new SendNotificationCommand(
                payer.TenantId,
                payer.CountryId,
                NotificationType.PaymentSucceeded,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    payer.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = payer.FirstName,
                        ["IllustrationUrl"] = NotificationIllustrationUrls.PaymentSucceeded,
                        ["TrackingId"] = delivery.TrackingId,
                        ["Amount"] = payment.VerifiedAmount.Value.ToString("N2", CultureInfo.InvariantCulture),
                        ["Currency"] = currency.CurrencyCode,
                        ["PaymentReference"] = payment.MerchantReference,
                        ["PaymentDate"] = paymentDate.ToString(
                            "dd MMM yyyy, h:mm tt 'WAT'",
                            CultureInfo.GetCultureInfo("en-NG")),
                        ["PaymentStatus"] = "Paid",
                        ["NextSteps"] = delivery.Status == DeliveryStatus.AwaitingDispatch
                            ? "Your scheduled delivery will enter rider matching when its dispatch window opens."
                            : "Your delivery is now eligible for rider matching.",
                        ["ActionUrl"] = actionUrl
                    }))
            {
                MessageId = NotificationMessageIdentity.Create(
                    payment.Id,
                    NotificationType.PaymentSucceeded,
                    payer.StakeholderId),
                RequestedAt = Clock.GetUtcNow(),
                StakeholderId = payer.StakeholderId,
                Language = payer.Language,
                FlowId = command.FlowId
            },
            cancellationToken);
    }
}
