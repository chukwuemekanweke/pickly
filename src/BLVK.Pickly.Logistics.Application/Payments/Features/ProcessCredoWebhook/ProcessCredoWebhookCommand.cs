namespace BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;

public sealed record ProcessCredoWebhookCommand(
    CredoWebhook Webhook,
    string RawPayload,
    string? SignatureHeader);

public sealed record CredoWebhook(string Event, CredoWebhookData Data);

public sealed record CredoWebhookData(
    string BusinessCode,
    string TransRef,
    string BusinessRef,
    decimal DebitedAmount,
    decimal TransAmount,
    decimal TransFeeAmount,
    decimal SettlementAmount,
    string CustomerId,
    string TransactionDate,
    int ChannelId,
    string CurrencyCode,
    int Status,
    string PaymentMethodType,
    string PaymentMethod,
    CredoWebhookCustomer Customer);

public sealed record CredoWebhookCustomer(
    string CustomerEmail,
    string FirstName,
    string LastName,
    string PhoneNumber);
