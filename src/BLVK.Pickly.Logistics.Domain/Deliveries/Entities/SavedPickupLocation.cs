using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

/// <summary>
/// A sender-owned association with a shared registry location. The canonical address and coordinates live only on
/// the referenced location; this record holds sender-private metadata (label, directions, default flag).
/// </summary>
public sealed class SavedPickupLocation : Entity, IAggregateRoot
{
    public const int MaxLabelLength = 100;
    public const int MaxAdditionalDirectionsLength = 500;

    private SavedPickupLocation()
    {
    }

    private SavedPickupLocation(
        Guid senderId,
        string label,
        Guid locationId,
        string? additionalDirections,
        bool isDefault)
    {
        SenderId = senderId;
        Label = label;
        LocationId = locationId;
        AdditionalDirections = NormalizeAdditionalDirections(additionalDirections);
        IsDefault = isDefault;
    }

    public Guid SenderId { get; private set; }
    public Guid LocationId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string? AdditionalDirections { get; private set; }
    public bool IsDefault { get; private set; }

    public static SavedPickupLocation Create(
        Guid senderId,
        string label,
        Guid locationId,
        string? additionalDirections,
        bool isDefault = false)
    {
        EnsureRequiredId(senderId, nameof(senderId));
        EnsureRequiredId(locationId, nameof(locationId));

        return new SavedPickupLocation(
            senderId,
            NormalizeLabel(label),
            locationId,
            additionalDirections,
            isDefault);
    }

    public void Update(string label, Guid locationId, string? additionalDirections)
    {
        EnsureRequiredId(locationId, nameof(locationId));
        Label = NormalizeLabel(label);
        LocationId = locationId;
        AdditionalDirections = NormalizeAdditionalDirections(additionalDirections);
    }

    public void SetAsDefault()
    {
        if (IsDeleted)
        {
            throw new AggregateStateException("A deleted pickup location cannot be selected as default.");
        }

        IsDefault = true;
    }

    public void ClearDefault() => IsDefault = false;

    private static string NormalizeLabel(string label)
    {
        var normalized = label?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Pickup location label is required.", nameof(label));
        }

        if (normalized.Length > MaxLabelLength)
        {
            throw new ArgumentException(
                $"Pickup location label must not exceed {MaxLabelLength} characters.",
                nameof(label));
        }

        return normalized;
    }

    private static string? NormalizeAdditionalDirections(string? additionalDirections)
    {
        var normalized = additionalDirections?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > MaxAdditionalDirectionsLength)
        {
            throw new ArgumentException(
                $"Additional directions must not exceed {MaxAdditionalDirectionsLength} characters.",
                nameof(additionalDirections));
        }

        return normalized;
    }

    private static void EnsureRequiredId(Guid id, string argumentName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", argumentName);
        }
    }
}
