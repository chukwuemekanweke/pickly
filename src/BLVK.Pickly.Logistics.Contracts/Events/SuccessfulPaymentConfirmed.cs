using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Contracts.Events;

public sealed record SuccessfulPaymentConfirmed : BaseEvent
{
    public Guid PaymentTransactionId { get; init; }
    public string MerchantReference { get; init; } = string.Empty;
    public PaymentIntent PaymentIntent { get; init; }
    public Guid PaymentProviderId { get; init; }
    public decimal Amount { get; init; }
    public Guid CurrencyId { get; init; }
}
