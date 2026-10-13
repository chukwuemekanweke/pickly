using System.Text.Json;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.Webhooks.Credo;

public sealed record CredoWebhookRequest(
    [property: JsonPropertyName("event")] string Event,
    JsonElement Data);

public sealed record CredoWebhookData(
    [property: JsonPropertyName("businessCode")] string BusinessCode,
    [property: JsonPropertyName("transRef")] string TransRef,
    [property: JsonPropertyName("businessRef")] string BusinessRef,
    [property: JsonPropertyName("debitedAmount")] decimal DebitedAmount,
    [property: JsonPropertyName("transAmount")] decimal TransAmount,
    [property: JsonPropertyName("transFeeAmount")] decimal TransFeeAmount,
    [property: JsonPropertyName("settlementAmount")] decimal SettlementAmount,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("transactionDate")] string TransactionDate,
    [property: JsonPropertyName("channelId")] int ChannelId,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("paymentMethodType")] string PaymentMethodType,
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("customer")] CredoWebhookCustomer Customer);

public sealed record CredoWebhookCustomer(
    [property: JsonPropertyName("customerEmail")] string CustomerEmail,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("phoneNo")] string PhoneNumber);
