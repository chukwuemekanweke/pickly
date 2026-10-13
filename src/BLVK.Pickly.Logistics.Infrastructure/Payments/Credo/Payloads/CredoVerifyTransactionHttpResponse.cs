using Newtonsoft.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo.Payloads;

internal sealed record CredoVerifyTransactionHttpResponse(
    [property: JsonProperty("status")] int Status,
    [property: JsonProperty("message")] string? Message,
    [property: JsonProperty("data")] CredoVerifyTransactionHttpResponseData? Data,
    [property: JsonProperty("execTime")] decimal? ExecTime,
    [property: JsonProperty("error")] IReadOnlyList<string>? Error);

internal sealed record CredoVerifyTransactionHttpResponseData(
    [property: JsonProperty("businessCode")] string? BusinessCode,
    [property: JsonProperty("transRef")] string? TransRef,
    [property: JsonProperty("businessRef")] string? BusinessRef,
    [property: JsonProperty("debitedAmount")] decimal DebitedAmount,
    [property: JsonProperty("transAmount")] decimal TransAmount,
    [property: JsonProperty("transFeeAmount")] decimal TransFeeAmount,
    [property: JsonProperty("settlementAmount")] decimal SettlementAmount,
    [property: JsonProperty("customerId")] string? CustomerId,
    [property: JsonProperty("transactionDate")] string? TransactionDate,
    [property: JsonProperty("channelId")] int ChannelId,
    [property: JsonProperty("currencyCode")] string? CurrencyCode,
    [property: JsonProperty("status")] int Status,
    [property: JsonProperty("metadata")] IReadOnlyList<CredoVerifyTransactionMetadataPayload>? Metadata);

internal sealed record CredoVerifyTransactionMetadataPayload(
    [property: JsonProperty("insightTag")] string? InsightTag,
    [property: JsonProperty("insightTagValue")] string? InsightTagValue,
    [property: JsonProperty("insightTagDisplay")] string? InsightTagDisplay);
