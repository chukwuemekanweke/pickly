using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryStatusHistory : Entity
{
    private DeliveryStatusHistory()
    {
    }

    private DeliveryStatusHistory(
        Guid deliveryId,
        DeliveryStatus? previousStatus,
        DeliveryStatus newStatus,
        string? reason,
        DateTimeOffset occurredAtUtc,
        string actor,
        string source)
    {
        DeliveryId = deliveryId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = NormalizeOptional(reason, 500, nameof(reason));
        OccurredAtUtc = occurredAtUtc;
        Actor = NormalizeRequired(actor, 200, nameof(actor));
        Source = NormalizeRequired(source, 100, nameof(source));
    }

    public Guid DeliveryId { get; private set; }
    public DeliveryStatus? PreviousStatus { get; private set; }
    public DeliveryStatus NewStatus { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string Actor { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;

    internal static DeliveryStatusHistory Create(
        Guid deliveryId,
        DeliveryStatus? previousStatus,
        DeliveryStatus newStatus,
        string? reason,
        DateTimeOffset occurredAtUtc,
        string actor,
        string source) =>
        new(deliveryId, previousStatus, newStatus, reason, occurredAtUtc, actor, source);

    private static string NormalizeRequired(string value, int maxLength, string argumentName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", argumentName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string argumentName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}
