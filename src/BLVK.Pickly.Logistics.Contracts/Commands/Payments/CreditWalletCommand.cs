namespace BLVK.Pickly.Logistics.Contracts.Commands.Payments;

public sealed record CreditWalletCommand(
    Guid PaymentTransactionId,
    string MerchantReference,
    decimal Amount,
    Guid CurrencyId) : BaseCommand;
