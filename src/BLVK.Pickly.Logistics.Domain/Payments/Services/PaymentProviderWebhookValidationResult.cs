using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record PaymentProviderWebhookValidationResult(
    SignatureValidationStatus SignatureValidationStatus,
    string? StatusChangeReason);
