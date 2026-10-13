using Newtonsoft.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo.Payloads;

internal sealed record CredoInitializeTransactionHttpResponse(
    [property: JsonProperty("status")] int Status,
    [property: JsonProperty("message")] string? Message,
    [property: JsonProperty("data")] CredoInitializeTransactionHttpResponseData? Data,
    [property: JsonProperty("execTime")] decimal? ExecTime,
    [property: JsonProperty("error")] IReadOnlyList<string>? Error);

internal sealed record CredoInitializeTransactionHttpResponseData(
    [property: JsonProperty("authorizationUrl")] string AuthorizationUrl,
    [property: JsonProperty("reference")] string Reference,
    [property: JsonProperty("credoReference")] string CredoReference,
    [property: JsonProperty("crn")] string? Crn);
