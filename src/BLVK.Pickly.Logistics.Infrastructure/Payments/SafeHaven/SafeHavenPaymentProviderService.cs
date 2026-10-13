using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Services;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

internal sealed class SafeHavenPaymentProviderService(
    ISafeHavenClient client,
    TimeProvider timeProvider) : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.SafeHaven;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await client.CreateVirtualAccountAsync(
            new SafeHavenCreateVirtualAccountRequest(
                ExternalReference: request.MerchantReference,
                AccountName: request.MerchantReference,
                Amount: request.Amount),
            cancellationToken);

        var virtualAccount = response.Data;
        var now = timeProvider.GetUtcNow();

        return new PaymentProviderInitiationResult(
            virtualAccount.Id,
            ProviderKey,
            PaymentMethodType.BankTransfer,
            now.AddMinutes(15),
            new Dictionary<string, string>
            {
                [SafeHavenKnownKeys.PaymentInstruction.AccountNumber] = virtualAccount.AccountNumber,
                [SafeHavenKnownKeys.PaymentInstruction.BankName] = virtualAccount.BankCode,
                [SafeHavenKnownKeys.PaymentInstruction.AccountName] = virtualAccount.AccountName,
                [SafeHavenKnownKeys.PaymentInstruction.ProviderReference] = virtualAccount.Id
            });
    }

    public async Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing);
        }

        var response = await client.GetVirtualAccountAsync(request.ProviderReference, cancellationToken);
        var virtualAccount = response.Data;

        if (string.Equals(virtualAccount.Status, SafeHavenVirtualAccountStatuses.Completed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(virtualAccount.Status, SafeHavenVirtualAccountStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess);
        }

        if (string.Equals(virtualAccount.Status, SafeHavenVirtualAccountStatuses.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Failed,
                request.ProviderReference,
                "provider_reported_failure",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure);
        }

        if (string.Equals(virtualAccount.Status, SafeHavenVirtualAccountStatuses.Expired, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Expired,
                request.ProviderReference,
                "provider_reported_expired",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedExpired);
        }

        return new PaymentProviderVerificationResult(
            PaymentProviderVerificationStatus.Processing,
            request.ProviderReference,
            null,
            KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing);
    }

}
