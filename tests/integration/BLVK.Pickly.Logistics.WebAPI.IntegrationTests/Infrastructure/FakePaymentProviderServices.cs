using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Services;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

internal sealed class FakeCredoPaymentProviderService : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.Credo;

    public Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderInitiationResult(
            $"cr_{Guid.CreateVersion7():N}",
            PaymentProviderKeys.Credo,
            PaymentMethodType.PaymentLink,
            null,
            new Dictionary<string, string> { ["paymentLink"] = "https://payments.integration.local/credo" }));

    public Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderVerificationResult(
            PaymentProviderVerificationStatus.Processing,
            request.ProviderReference,
            null,
            null));

}

internal sealed class FakeSafeHavenPaymentProviderService : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.SafeHaven;

    public Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderInitiationResult(
            $"sh_{Guid.CreateVersion7():N}",
            PaymentProviderKeys.SafeHaven,
            PaymentMethodType.BankTransfer,
            DateTimeOffset.UtcNow.AddMinutes(30),
            new Dictionary<string, string>
            {
                ["accountNumber"] = "1234567890",
                ["bankName"] = "SafeHaven Demo Bank"
            }));

    public Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentProviderVerificationResult(
            PaymentProviderVerificationStatus.Processing,
            request.ProviderReference,
            null,
            null));

}
