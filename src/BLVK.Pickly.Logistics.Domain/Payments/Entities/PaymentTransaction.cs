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
        PaymentStatus is PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Cancelled or PaymentStatus.Expired;

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

    public void MarkFailed(
        string? providerReference,
        string? failureReason,
        string? statusChangeReason,
        DateTimeOffset failedAtUtc)
    {
        EnsureCanTransitionFrom(PaymentStatus.Initiated);
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
        EnsureCanTransitionFrom(PaymentStatus.Initiated);
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? ProviderReference : providerReference.Trim();
        PaymentStatus = PaymentStatus.Expired;
        FailureReason = failureReason;
        StatusChangeReason = statusChangeReason;
        CompletedAtUtc = null;
        FailedAtUtc = null;
    }

    public void RecordStatusCheck(DateTimeOffset utcNow) => LastStatusCheckAtUtc = utcNow;

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
