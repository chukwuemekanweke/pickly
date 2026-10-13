namespace BLVK.Pickly.Logistics.Application.Payments.Features.ActivatePaymentProvider;

public sealed record ActivatePaymentProviderCommand(Guid PaymentProviderId, bool IsActive);
