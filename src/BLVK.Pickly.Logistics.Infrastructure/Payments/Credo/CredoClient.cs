using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo.Payloads;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal sealed class CredoClient(
    IHttpClientFactory httpClientFactory,
    IOptions<CredoOptions> options) : ICredoClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.Credo);
    private readonly CredoOptions _options = options.Value;

    public Task<CredoInitializeTransactionResponse> InitializeTransactionAsync(
        CredoInitializeTransactionRequest request,
        CancellationToken cancellationToken) =>
        InitializeTransactionCoreAsync(request, cancellationToken);

    public Task<CredoVerifyTransactionResponse> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken) =>
        VerifyTransactionCoreAsync(reference, cancellationToken);

    private async Task<CredoInitializeTransactionResponse> InitializeTransactionCoreAsync(
        CredoInitializeTransactionRequest request,
        CancellationToken cancellationToken)
    {
        ApplyAuthorizationHeader(_options.PublicKey);

        using var response = await _httpClient.PostAsJsonAsync(
            "/transaction/initialize",
            CreatePayload(request),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsAsync<CredoInitializeTransactionHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Credo initialize transaction response was empty.");

        if (payload.Data is null)
        {
            throw new InvalidOperationException("Credo initialize transaction response did not contain data.");
        }

        return new CredoInitializeTransactionResponse(
            payload.Data.AuthorizationUrl,
            payload.Data.Reference,
            payload.Data.CredoReference,
            payload.Data.Crn);
    }

    private async Task<CredoVerifyTransactionResponse> VerifyTransactionCoreAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        ApplyAuthorizationHeader(_options.SecretKey);

        using var response = await _httpClient.GetAsync(
            $"/transaction/{Uri.EscapeDataString(reference)}/verify",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsAsync<CredoVerifyTransactionHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Credo verify transaction response was empty.");

        if (payload.Data is null)
        {
            throw new InvalidOperationException("Credo verify transaction response did not contain data.");
        }

        return new CredoVerifyTransactionResponse(
            payload.Message,
            payload.Data.BusinessCode,
            payload.Data.TransRef,
            payload.Data.BusinessRef,
            payload.Data.DebitedAmount,
            payload.Data.TransAmount,
            payload.Data.TransFeeAmount,
            payload.Data.SettlementAmount,
            payload.Data.CustomerId,
            payload.Data.TransactionDate,
            payload.Data.ChannelId,
            payload.Data.CurrencyCode,
            payload.Data.Status,
            payload.Data.Metadata?.Select(item => new CredoMetadataEntry(
                item.InsightTag,
                item.InsightTagValue,
                item.InsightTagDisplay)).ToArray());
    }

    private CredoInitializeTransactionPayload CreatePayload(CredoInitializeTransactionRequest request) =>
        new(
            request.Amount,
            request.Email,
            request.CustomerPhoneNumber,
            request.CustomerFirstName,
            request.CustomerLastName,
            request.Currency,
            request.Reference,
            _options.CallbackUrl,
            _options.Channels,
            _options.Bearer,
            new Dictionary<string, string>(),
            request.Narration,
            _options.InitializeAccount);

    private void ApplyAuthorizationHeader(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Credo API key is not configured.");
        }

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey.Trim());
    }
}
