using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class PaymentTransaction : Entity, IAggregateRoot
{
    private PaymentTransaction()
    {
    }

    private PaymentTransaction(
        string merchantReference,
        PaymentIntent paymentIntent,
        PaymentStatus paymentStatus,
        Guid paymentProviderId,
        decimal amount,
        Guid currencyId,
        Guid countryId,
        Guid initiatedByUserId,
        Guid stakeholderId,
        Guid tenantId)
    {
        MerchantReference = merchantReference;
        PaymentIntent = paymentIntent;
        PaymentStatus = paymentStatus;
        PaymentProviderId = paymentProviderId;
        Amount = amount;
        CurrencyId = currencyId;
        CountryId = countryId;
        InitiatedByUserId = initiatedByUserId;
        StakeholderId = stakeholderId;
        TenantId = tenantId;
    }

    public string MerchantReference { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public PaymentIntent PaymentIntent { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public Guid PaymentProviderId { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public Guid CountryId { get; private set; }
    public Guid InitiatedByUserId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal? VerifiedAmount { get; private set; }
    public Guid? VerifiedCurrencyId { get; private set; }
    public string? VerifiedCurrencyCode { get; private set; }
    public DateTimeOffset? ProviderVerifiedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public string? StatusChangeReason { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public DateTimeOffset? LastStatusCheckAtUtc { get; private set; }
    public PaymentMethodType PaymentMethodType { get; private set; }
    public Dictionary<string, string> ProviderPayloadMetadata { get; private set; } = [];
    public uint RowVersion { get; private set; }

    public static PaymentTransaction Create(
        string merchantReference,
        PaymentIntent paymentIntent,
        Guid paymentProviderId,
        decimal amount,
        Guid currencyId,
        Guid countryId,
        Guid initiatedByUserId,
        Guid stakeholderId,
        Guid tenantId) =>
        new(
            merchantReference.Trim(),
            paymentIntent,
            PaymentStatus.PendingInitiation,
            paymentProviderId,
            amount,
            currencyId,
            countryId,
            initiatedByUserId,
            stakeholderId,
            tenantId);

    public bool HasReachedTerminalState() =>
        PaymentStatus is PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Expired or PaymentStatus.Unresolved;

    public void MarkInitiated(
        string providerReference,
        Dictionary<string, string>? providerPayloadMetadata,
        DateTimeOffset? expiresAtUtc,
        string? statusChangeReason)
    {
        EnsureCanTransitionFrom(PaymentStatus.PendingInitiation);
        ProviderReference = providerReference.Trim();
        PaymentStatus = PaymentStatus.Initiated;
        ProviderPayloadMetadata = providerPayloadMetadata ?? [];
        ExpiresAtUtc = expiresAtUtc;
        StatusChangeReason = statusChangeReason;
        FailureReason = null;
        FailedAtUtc = null;
    }

    public void SetPaymentMethodType(PaymentMethodType paymentMethodType)
    {
        PaymentMethodType = paymentMethodType;
    }

    public void MarkSucceeded(
        string? providerReference,
        string? statusChangeReason,
        DateTimeOffset completedAtUtc)
    {
        EnsureCanTransitionFrom(PaymentStatus.Initiated);
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        PaymentStatus = PaymentStatus.Succeeded;
        StatusChangeReason = statusChangeReason;
        CompletedAtUtc = completedAtUtc;
        FailedAtUtc = null;
        FailureReason = null;
    }

    public void ConfirmVerifiedSettlement(
        string? providerReference,
        decimal verifiedAmount,
        Guid verifiedCurrencyId,
        DateTimeOffset verifiedAtUtc,
        string? statusChangeReason,
        string? verifiedCurrencyCode = null)
    {
        EnsureCanTransitionFrom(PaymentStatus.PendingInitiation, PaymentStatus.Initiated);
        ConfirmVerifiedSettlementCore(
            providerReference,
            verifiedAmount,
            verifiedCurrencyId,
            verifiedAtUtc,
            statusChangeReason,
            verifiedCurrencyCode);
    }

    public void ConfirmLateVerifiedSettlement(
        string? providerReference,
        decimal verifiedAmount,
        Guid verifiedCurrencyId,
        DateTimeOffset verifiedAtUtc,
        string? statusChangeReason,
        string? verifiedCurrencyCode = null)
    {
        if (PaymentIntent != PaymentIntent.DeliveryPayment ||
            PaymentStatus is not (PaymentStatus.Failed or PaymentStatus.Expired))
        {
            throw new AggregateStateException(
                $"Payment transaction '{MerchantReference}' cannot record a late settlement from '{PaymentStatus}'.");
        }

        ConfirmVerifiedSettlementCore(
            providerReference,
            verifiedAmount,
            verifiedCurrencyId,
            verifiedAtUtc,
            statusChangeReason,
            verifiedCurrencyCode);
    }

    public void MarkUnresolved(
        string? providerReference,
        decimal? verifiedAmount,
        Guid? verifiedCurrencyId,
        string? verifiedCurrencyCode,
        DateTimeOffset? verifiedAtUtc,
        string reason,
        string? statusChangeReason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An unresolved settlement reason is required.", nameof(reason));
        }

        var isLateDeliverySettlement =
            PaymentIntent == PaymentIntent.DeliveryPayment &&
            PaymentStatus is PaymentStatus.Failed or PaymentStatus.Expired;
        if (!isLateDeliverySettlement)
        {
            EnsureCanTransitionFrom(PaymentStatus.PendingInitiation, PaymentStatus.Initiated);
        }

        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        PaymentStatus = PaymentStatus.Unresolved;
        VerifiedAmount = verifiedAmount;
        VerifiedCurrencyId = verifiedCurrencyId;
        VerifiedCurrencyCode = NormalizeCurrencyCode(verifiedCurrencyCode);
        ProviderVerifiedAtUtc = verifiedAtUtc?.ToUniversalTime();
        FailureReason = reason.Trim();
        StatusChangeReason = statusChangeReason;
        CompletedAtUtc = null;
        FailedAtUtc = null;
    }

    public void MarkFailed(
        string? providerReference,
        string? failureReason,
        string? statusChangeReason,
        DateTimeOffset failedAtUtc)
    {
        EnsureCanTransitionFrom(PaymentStatus.PendingInitiation, PaymentStatus.Initiated);
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        PaymentStatus = PaymentStatus.Failed;
        FailureReason = failureReason;
        StatusChangeReason = statusChangeReason;
        FailedAtUtc = failedAtUtc;
    }

    public void MarkExpired(
        string? providerReference,
        string? failureReason,
        string? statusChangeReason)
    {
        EnsureCanTransitionFrom(PaymentStatus.PendingInitiation, PaymentStatus.Initiated);
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        PaymentStatus = PaymentStatus.Expired;
        FailureReason = failureReason;
        StatusChangeReason = statusChangeReason;
        CompletedAtUtc = null;
        FailedAtUtc = null;
    }

    public void RecordStatusCheck(DateTimeOffset utcNow) => LastStatusCheckAtUtc = utcNow;

    private void ConfirmVerifiedSettlementCore(
        string? providerReference,
        decimal verifiedAmount,
        Guid verifiedCurrencyId,
        DateTimeOffset verifiedAtUtc,
        string? statusChangeReason,
        string? verifiedCurrencyCode)
    {
        if (verifiedAmount != Amount || verifiedCurrencyId != CurrencyId)
        {
            throw new AggregateStateException("Verified settlement must exactly match the requested amount and currency.");
        }

        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        PaymentStatus = PaymentStatus.Succeeded;
        VerifiedAmount = verifiedAmount;
        VerifiedCurrencyId = verifiedCurrencyId;
        VerifiedCurrencyCode = NormalizeCurrencyCode(verifiedCurrencyCode);
        ProviderVerifiedAtUtc = verifiedAtUtc.ToUniversalTime();
        StatusChangeReason = statusChangeReason;
        CompletedAtUtc = verifiedAtUtc.ToUniversalTime();
        FailedAtUtc = null;
        FailureReason = null;
    }

    private static string? NormalizeCurrencyCode(string? currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant();

    private void EnsureCanTransitionFrom(params PaymentStatus[] allowedStatuses)
    {
        if (HasReachedTerminalState())
        {
            throw new AggregateStateException(
                $"Payment transaction '{MerchantReference}' is already in terminal state '{PaymentStatus}'.");
        }

        if (allowedStatuses.Contains(PaymentStatus))
        {
            return;
        }

        throw new AggregateStateException(
            $"Payment transaction '{MerchantReference}' cannot transition from '{PaymentStatus}'.");
    }
}
