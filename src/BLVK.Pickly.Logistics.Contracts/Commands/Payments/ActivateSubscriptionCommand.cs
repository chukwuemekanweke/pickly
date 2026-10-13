namespace BLVK.Pickly.Logistics.Contracts.Commands.Payments;

public sealed record ActivateSubscriptionCommand(
    Guid PaymentTransactionId,
    string MerchantReference,
    decimal Amount,
    Guid CurrencyId) : BaseCommand;
