namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record CredoWebhookSignatureValidationRequest(
    string? SignatureHeader,
    string? BusinessCode);
