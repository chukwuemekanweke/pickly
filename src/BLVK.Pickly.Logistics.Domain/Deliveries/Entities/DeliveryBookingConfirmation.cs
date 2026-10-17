using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryBookingConfirmation : Entity, IAggregateRoot
{
    public const string OperationName = "create_delivery";

    private DeliveryBookingConfirmation()
    {
    }

    private DeliveryBookingConfirmation(
        Guid senderId,
        Guid deliveryQuoteId,
        Guid deliveryId,
        string keyHash,
        string requestFingerprint,
        DateTimeOffset expiresAtUtc)
    {
        SenderId = senderId;
        DeliveryQuoteId = deliveryQuoteId;
        DeliveryId = deliveryId;
        KeyHash = NormalizeHash(keyHash, nameof(keyHash));
        RequestFingerprint = NormalizeHash(requestFingerprint, nameof(requestFingerprint));
        Operation = OperationName;
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
    }

    public Guid SenderId { get; private set; }
    public Guid DeliveryQuoteId { get; private set; }
    public Guid DeliveryId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public static DeliveryBookingConfirmation Create(
        Guid senderId,
        Guid deliveryQuoteId,
        Guid deliveryId,
        string keyHash,
        string requestFingerprint,
        DateTimeOffset expiresAtUtc)
    {
        if (senderId == Guid.Empty || deliveryQuoteId == Guid.Empty || deliveryId == Guid.Empty)
        {
            throw new ArgumentException("Sender, quote and delivery identifiers are required.");
        }

        return new(senderId, deliveryQuoteId, deliveryId, keyHash, requestFingerprint, expiresAtUtc);
    }

    private static string NormalizeHash(string value, string argumentName)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 64 || normalized.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new ArgumentException("A SHA-256 hash is required.", argumentName);
        }

        return normalized;
    }
}
