namespace BLVK.Pickly.Logistics.Application.Payments.Features.ActivatePaymentProvider;

public sealed record ActivatePaymentProviderResult(ActivatePaymentProviderStatus Status, string? Error = null);
