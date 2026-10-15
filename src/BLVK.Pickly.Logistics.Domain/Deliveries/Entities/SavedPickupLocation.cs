using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class SavedPickupLocation : Entity, IAggregateRoot
{
    public const int MaxLabelLength = 100;

    private SavedPickupLocation()
    {
    }

    private SavedPickupLocation(
        Guid senderId,
        string label,
        DeliveryAddress address,
        bool isDefault)
    {
        SenderId = senderId;
        Label = label;
        Address = address;
        IsDefault = isDefault;
    }

    public Guid SenderId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public DeliveryAddress Address { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    public static SavedPickupLocation Create(
        Guid senderId,
        string label,
        DeliveryAddress address,
        bool isDefault = false)
    {
        EnsureRequiredId(senderId, nameof(senderId));
        ArgumentNullException.ThrowIfNull(address);

        return new SavedPickupLocation(
            senderId,
            NormalizeLabel(label),
            address,
            isDefault);
    }

    public void Update(string label, DeliveryAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        Label = NormalizeLabel(label);
        Address = address;
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

    private static void EnsureRequiredId(Guid id, string argumentName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", argumentName);
        }
    }
}
