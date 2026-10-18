using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryPaymentAttempt : Entity, IAggregateRoot
{
    private DeliveryPaymentAttempt()
    {
    }

    private DeliveryPaymentAttempt(Guid deliveryId, Guid paymentTransactionId, string? idempotencyKey)
    {
        DeliveryId = deliveryId;
        PaymentTransactionId = paymentTransactionId;
        IdempotencyKey = idempotencyKey;
    }

    public Guid DeliveryId { get; private set; }
    public Guid PaymentTransactionId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset? AppliedAtUtc { get; private set; }
    public string? ApplicationException { get; private set; }

    public static DeliveryPaymentAttempt Create(
        Guid deliveryId,
        Guid paymentTransactionId,
        string? idempotencyKey)
    {
        if (deliveryId == Guid.Empty)
        {
            throw new ArgumentException("Delivery identifier is required.", nameof(deliveryId));
        }

        if (paymentTransactionId == Guid.Empty)
        {
            throw new ArgumentException("Payment transaction identifier is required.", nameof(paymentTransactionId));
        }

        var normalizedKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (normalizedKey?.Length > 200)
        {
            throw new ArgumentException("Idempotency key cannot exceed 200 characters.", nameof(idempotencyKey));
        }

        return new DeliveryPaymentAttempt(deliveryId, paymentTransactionId, normalizedKey);
    }

    public void RecordApplied(DateTimeOffset appliedAtUtc)
    {
        AppliedAtUtc ??= appliedAtUtc.ToUniversalTime();
        ApplicationException = null;
    }

    public void RecordApplicationException(string reason) =>
        ApplicationException = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException("An exception reason is required.", nameof(reason))
            : reason.Trim();
}
