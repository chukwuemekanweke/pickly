namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal interface ICredoClient
{
    Task<CredoInitializeTransactionResponse> InitializeTransactionAsync(
        CredoInitializeTransactionRequest request,
        CancellationToken cancellationToken);

    Task<CredoVerifyTransactionResponse> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken);
}
