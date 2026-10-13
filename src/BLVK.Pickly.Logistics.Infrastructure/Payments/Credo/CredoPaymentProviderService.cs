using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal sealed class CredoPaymentProviderService(
    ICredoClient client,
    IStakeholderReadModelRepository stakeholderReadModelRepository) : IPaymentProviderService
{
    public string ProviderKey => PaymentProviderKeys.Credo;

    public async Task<PaymentProviderInitiationResult> InitiatePaymentAsync(
        PaymentProviderInitiationRequest request,
        CancellationToken cancellationToken)
    {
        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(request.StakeholderId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Unable to resolve stakeholder '{request.StakeholderId}' for Credo payment initiation.");

        var response = await client.InitializeTransactionAsync(
            new CredoInitializeTransactionRequest(
                ConvertAmount(request.Amount),
                string.IsNullOrWhiteSpace(stakeholder.EmailAddress)
                    ? throw new InvalidOperationException(
                        $"Stakeholder '{stakeholder.StakeholderId}' does not have an email for Credo payment initiation.")
                    : stakeholder.EmailAddress.Trim(),
                null,
                stakeholder.FirstName,
                stakeholder.LastName,
                request.CurrencyCode,
                request.MerchantReference,
                $"{request.PaymentIntent} payment"),
            cancellationToken);

        var providerReference = NormalizeRequired(
            response.CredoReference,
            "Credo initialize transaction response did not contain a credo reference.");
        var merchantReference = NormalizeRequired(
            response.Reference,
            "Credo initialize transaction response did not contain a merchant reference.");
        var paymentLink = NormalizeRequired(
            response.AuthorizationUrl,
            "Credo initialize transaction response did not contain an authorization URL.");

        return new PaymentProviderInitiationResult(
            providerReference,
            ProviderKey,
            PaymentMethodType.PaymentLink,
            null,
            new Dictionary<string, string>
            {
                [CredoKnownKeys.PaymentInstruction.PaymentLink] = paymentLink,
                [CredoKnownKeys.PaymentInstruction.ProviderReference] = providerReference,
                [CredoKnownKeys.PaymentInstruction.MerchantReference] = merchantReference
            });
    }

    public async Task<PaymentProviderVerificationResult> VerifyPaymentAsync(
        PaymentProviderVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await client.VerifyTransactionAsync(request.MerchantReference, cancellationToken);

        return response.Status switch
        {
            CredoTransactionStatuses.Successful or CredoTransactionStatuses.Settle or CredoTransactionStatuses.Settled
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Succeeded,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    null,
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess),
            CredoTransactionStatuses.Failed or CredoTransactionStatuses.Declined
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    NormalizeFailureReason(response.Message, response.Status),
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedFailure),
            CredoTransactionStatuses.CancelledByCustomer
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    NormalizeFailureReason(response.Message, response.Status),
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedCustomerCancellation),
            CredoTransactionStatuses.CancelledByMerchant
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    NormalizeFailureReason(response.Message, response.Status),
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedMerchantCancellation),
            CredoTransactionStatuses.Refunded
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    NormalizeFailureReason(response.Message, response.Status),
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedRefund),
            CredoTransactionStatuses.Refund
                => new PaymentProviderVerificationResult(
                    PaymentProviderVerificationStatus.Failed,
                    NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                    NormalizeFailureReason(response.Message, response.Status),
                    KnownPaymentTransactionChangeReasons.ReconciliationConfirmedRefundQueued),
            _ => new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                NormalizeOptional(response.TransRef) ?? request.ProviderReference,
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing)
        };
    }

    private static long ConvertAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Credo amount must be greater than zero.");
        }

        if (decimal.Truncate(amount) != amount)
        {
            throw new InvalidOperationException("Credo amount must be provided in the lowest currency unit.");
        }

        return decimal.ToInt64(amount);
    }

    private static string NormalizeRequired(string? value, string errorMessage) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(errorMessage)
            : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeFailureReason(string? message, int status) =>
        string.IsNullOrWhiteSpace(message)
            ? $"provider_status_{status}"
            : message.Trim();
}
