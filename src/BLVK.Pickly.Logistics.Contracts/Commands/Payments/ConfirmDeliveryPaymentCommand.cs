namespace BLVK.Pickly.Logistics.Contracts.Commands.Payments;

public sealed record ConfirmDeliveryPaymentCommand(
    Guid PaymentTransactionId,
    string MerchantReference,
    decimal Amount,
    Guid CurrencyId) : BaseCommand;
