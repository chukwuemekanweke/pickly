namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record PaymentProviderVerificationResult(
    PaymentProviderVerificationStatus VerificationStatus,
    string? ProviderReference,
    string? FailureReason,
    string? StatusChangeReason);
