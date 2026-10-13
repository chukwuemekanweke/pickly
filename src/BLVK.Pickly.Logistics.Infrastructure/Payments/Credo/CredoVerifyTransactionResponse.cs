namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal sealed record CredoVerifyTransactionResponse(
    string? Message,
    string? BusinessCode,
    string? TransRef,
    string? BusinessRef,
    decimal DebitedAmount,
    decimal TransAmount,
    decimal TransFeeAmount,
    decimal SettlementAmount,
    string? CustomerId,
    string? TransactionDate,
    int ChannelId,
    string? CurrencyCode,
    int Status,
    IReadOnlyList<CredoMetadataEntry>? Metadata);

internal sealed record CredoMetadataEntry(
    string? InsightTag,
    string? InsightTagValue,
    string? InsightTagDisplay);
