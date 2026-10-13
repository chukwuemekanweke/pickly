namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public interface IPaymentProviderService
{
    string ProviderKey { get; }
    Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken);
    Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken);
}
