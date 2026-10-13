using BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven.Payloads;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;

internal sealed class SafeHavenClient(
    IHttpClientFactory httpClientFactory,
    IOptions<SafeHavenOptions> options,
    TimeProvider timeProvider) : ISafeHavenClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PaymentHttpClientNames.SafeHaven);
    private readonly SafeHavenOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    private SafeHavenTokenResponse? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public async Task<SafeHavenResponse<SafeHavenVirtualAccount>> CreateVirtualAccountAsync(
        SafeHavenCreateVirtualAccountRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new SafeHavenCreateVirtualAccountPayload(
            AccountName: request.AccountName,
            ValidFor: _options.ValidFor,
            SettlementAccount: new SafeHavenSettlementAccountPayload(
                BankCode: _options.SettlementBankCode,
                AccountNumber: _options.SettlementAccountNumber),
            AmountControl: "fixed",
            Amount: request.Amount,
            CallbackUrl: _options.CallbackUrl,
            ExternalReference: request.ExternalReference);

        using var response = await PostAsJsonAsync("/virtual-accounts", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenVirtualAccount>>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven create virtual account response was empty.");
    }

    public async Task<SafeHavenResponse<SafeHavenVirtualAccount>> GetVirtualAccountAsync(
        string virtualAccountId,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/virtual-accounts/{virtualAccountId}");
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<SafeHavenResponse<SafeHavenVirtualAccount>>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven get virtual account response was empty.");
    }

    public async Task<SafeHavenResponse<SafeHavenIdentityVerification>> InitiateIdentityVerificationAsync(
        SafeHavenInitiateVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new SafeHavenInitiateVerificationPayload(
            Type: request.Type,
            Number: request.Number,
            DebitAccountNumber: request.DebitAccountNumber,
            Async: false);

        using var response = await PostAsJsonAsync("/identity/v2", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenIdentityVerification>>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven identity verification initiation response was empty.");
    }

    public async Task<SafeHavenResponse<SafeHavenIdentityVerification>> ValidateIdentityVerificationAsync(
        SafeHavenValidateVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        using var response = await PostAsJsonAsync("/identity/v2/validate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenIdentityVerification>>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven identity verification validation response was empty.");
    }

    public async Task<SafeHavenResponse<SafeHavenSubAccount>> CreateSubAccountAsync(
        SafeHavenCreateSubAccountRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var payload = new SafeHavenCreateSubAccountPayload(
            PhoneNumber: request.PhoneNumber,
            Email: request.Email,
            ExternalReference: request.ExternalReference,
            IdentityType: request.IdentityType,
            IdentityNumber: request.IdentityNumber,
            IdentityId: request.IdentityId,
            Otp: request.Otp,
            CallbackUrl: _options.CallbackUrl,
            AutoSweep: true,
            AutoSweepDetails: new SafeHavenCreateSubAccountAutoSweepDetailsPayload(
                AccountNumber: _options.AutoSweepAccountNumber,
                Schedule: "Instant"));

        using var response = await PostAsJsonAsync("/accounts/v2/subaccount", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SafeHavenResponse<SafeHavenSubAccount>>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven create sub-account response was empty.");
    }

    public async Task<SafeHavenResponse<IReadOnlyList<SafeHavenAccountStatementEntry>>> GetAccountStatementAsync(
        SafeHavenAccountStatementRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var queryParams = new List<string>();
        if (request.Page > 0)
        {
            queryParams.Add($"page={request.Page}");
        }
        if (request.Limit > 0)
        {
            queryParams.Add($"limit={request.Limit}");
        }
        if (request.FromDate.HasValue)
        {
            queryParams.Add($"fromDate={Uri.EscapeDataString(request.FromDate.Value.ToShortDateString())}");
        }
        if (request.ToDate.HasValue)
        {
            queryParams.Add($"toDate={Uri.EscapeDataString(request.ToDate.Value.ToShortDateString())}");
        }
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            queryParams.Add($"type={Uri.EscapeDataString(request.Type)}");
        }

        var uri = $"/accounts/{request.AccountId}/statement";
        if (queryParams.Count > 0)
        {
            uri += "?" + string.Join("&", queryParams);
        }

        using var httpRequest = CreateAuthenticatedRequest(HttpMethod.Get, uri);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SafeHavenResponse<IReadOnlyList<SafeHavenAccountStatementEntry>>>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven account statement response was empty.");
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && _tokenExpiry > _timeProvider.GetUtcNow().AddMinutes(1))
        {
            return;
        }

        var request = new SafeHavenTokenRequest(
            "client_credentials",
            _options.ClientId,
            _options.ClientAssertion,
            "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");

        var token = await ExchangeTokenAsync(request, cancellationToken);

        _cachedToken = token;
        _tokenExpiry = _timeProvider.GetUtcNow().AddSeconds(token.ExpiresIn - 60);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken!.AccessToken);
        request.Headers.TryAddWithoutValidation("ClientID", _cachedToken.IbsClientId);
        return request;
    }

    private Task<HttpResponseMessage> PostAsJsonAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        ApplyAuthenticationHeaders();
        return System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(_httpClient, requestUri, request, cancellationToken);
    }

    private void ApplyAuthenticationHeaders()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken!.AccessToken);
        _httpClient.DefaultRequestHeaders.Remove("ClientID");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("ClientID", _cachedToken.IbsClientId);
    }

    private Task<SafeHavenTokenResponse> ExchangeTokenAsync(
        SafeHavenTokenRequest request,
        CancellationToken cancellationToken) =>
        ExchangeTokenCoreAsync(request, cancellationToken);

    private async Task<SafeHavenTokenResponse> ExchangeTokenCoreAsync(
        SafeHavenTokenRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(_httpClient, "/oauth2/token", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SafeHavenTokenResponse>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("SafeHaven token response was empty.");
    }

}
