namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record PaymentProviderVerificationResult(
    PaymentProviderVerificationStatus VerificationStatus,
    string? ProviderReference,
    decimal? VerifiedAmount,
    string? VerifiedCurrencyCode,
    DateTimeOffset? VerifiedAtUtc,
    string? FailureReason,
    string? StatusChangeReason)
{
    public PaymentProviderVerificationResult(
        PaymentProviderVerificationStatus verificationStatus,
        string? providerReference,
        string? failureReason,
        string? statusChangeReason)
        : this(verificationStatus, providerReference, null, null, null, failureReason, statusChangeReason)
    {
    }
}
