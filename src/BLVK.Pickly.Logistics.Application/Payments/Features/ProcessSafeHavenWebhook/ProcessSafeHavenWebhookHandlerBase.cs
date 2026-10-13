using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessPaymentWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;

public abstract class ProcessSafeHavenWebhookHandlerBase<TData>(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly ActorContext AnonymousActorContext = new(null, null, string.Empty, string.Empty);

    public async Task<ProcessPaymentWebhookResult> HandleAsync(
        ProcessSafeHavenWebhookCommand<TData> command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByKeySpecification(PaymentProviderKeys.SafeHaven),
                cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven payment provider is not active.");
        var validationResult = new PaymentProviderWebhookValidationResult(
            SignatureValidationStatus.NotApplicable,
            KnownWebhookStatusChangeReasons.Payments.SignatureNotApplicable);
        var webhookDetails = CreateWebhookDetails(command.Webhook);

        if (!string.IsNullOrWhiteSpace(webhookDetails.WebhookEventId))
        {
            var existingWebhook = await paymentWebhookInboxRepository.FirstOrDefaultAsync(
                new PaymentWebhookInboxByEventIdSpecification(paymentProvider.Id, webhookDetails.WebhookEventId),
                cancellationToken);
            if (existingWebhook is not null)
            {
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.WebhookPersistenceFailed,
                    ObservabilityEventProperties.Create(
                        AnonymousActorContext,
                        failureReason: ObservabilityFailureReasons.DuplicateProcessing,
                        additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));
                return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Duplicate);
            }
        }

        var inbox = PaymentWebhookInbox.Create(
            paymentProvider.Id,
            webhookDetails.MerchantReference,
            webhookDetails.ProviderReference,
            webhookDetails.WebhookEventName,
            webhookDetails.WebhookEventId,
            command.RawPayload,
            validationResult.SignatureValidationStatus,
            validationResult.StatusChangeReason,
            now);
        await paymentWebhookInboxRepository.AddAsync(inbox, cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WebhookReceived,
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));

        if (validationResult.SignatureValidationStatus == SignatureValidationStatus.Invalid)
        {
            inbox.MarkIgnored(KnownWebhookStatusChangeReasons.Shared.InvalidSignature, now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WebhookPersistenceFailed,
                ObservabilityEventProperties.Create(
                    AnonymousActorContext,
                    failureReason: ObservabilityFailureReasons.InvalidSignature,
                    additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.InvalidSignature);
        }

        var paymentTransaction = await ResolvePaymentTransactionAsync(webhookDetails, cancellationToken);
        if (paymentTransaction is null || !webhookDetails.IsSupportedEvent)
        {
            inbox.MarkIgnored(KnownWebhookStatusChangeReasons.Payments.TransactionNotFoundOrUnmappedStatus, now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.WebhookPersistenceFailed,
                ObservabilityEventProperties.Create(
                    AnonymousActorContext,
                    failureReason: ObservabilityFailureReasons.TransactionNotFoundOrUnmappedStatus,
                    additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, webhookDetails.MerchantReference)));
            return new ProcessPaymentWebhookResult(WebhookReceiptStatus.UnidentifiedTransaction);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.WebhookPersisted,
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                paymentTransaction.StakeholderId,
                additionalProperties: CreateWebhookProperties(paymentProvider.ProviderKey, paymentTransaction.MerchantReference)));

        return new ProcessPaymentWebhookResult(WebhookReceiptStatus.Persisted);
    }

    protected abstract SafeHavenWebhookDetails CreateWebhookDetails(SafeHavenWebhook<TData> webhook);

    protected static string GetRequiredEventName(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("SafeHaven webhook event name is required.")
            : value.Trim();

    protected static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static string? CreateWebhookEventId(string? merchantReference, string eventName) =>
        string.IsNullOrWhiteSpace(merchantReference) ? null : $"{merchantReference.Trim()}:{eventName}";

    private static Dictionary<string, string> CreateWebhookProperties(string provider, string? paymentReference) =>
        string.IsNullOrWhiteSpace(paymentReference)
            ? new Dictionary<string, string>
            {
                [Observability.PropertyNames.Payments.Provider] = provider
            }
            : new Dictionary<string, string>
            {
                [Observability.PropertyNames.Payments.Provider] = provider,
                [Observability.PropertyNames.Payments.PaymentReference] = paymentReference
            };

    private async Task<PaymentTransaction?> ResolvePaymentTransactionAsync(
        SafeHavenWebhookDetails webhookDetails,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(webhookDetails.MerchantReference))
        {
            var transactionByMerchantReference = await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByMerchantReferenceSpecification(webhookDetails.MerchantReference),
                cancellationToken);
            if (transactionByMerchantReference is not null)
            {
                return transactionByMerchantReference;
            }
        }

        if (!string.IsNullOrWhiteSpace(webhookDetails.ProviderReference))
        {
            return await paymentTransactionRepository.FirstOrDefaultAsync(
                new PaymentTransactionByProviderReferenceSpecification(webhookDetails.ProviderReference),
                cancellationToken);
        }

        return null;
    }
}
