namespace BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven.Payloads;

internal sealed record SafeHavenCreateVirtualAccountPayload(
    string AccountName,
    int ValidFor,
    SafeHavenSettlementAccountPayload SettlementAccount,
    string AmountControl,
    decimal Amount,
    string CallbackUrl,
    string ExternalReference);
