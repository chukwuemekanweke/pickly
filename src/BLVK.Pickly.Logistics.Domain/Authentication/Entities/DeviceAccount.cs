using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Entities;

public sealed class DeviceAccount : Entity, IAggregateRoot
{
    public const int MaxIdentifierLength = 256;

    private DeviceAccount()
    {
    }

    private DeviceAccount(string identifier, Guid appUserId, Guid stakeholderId)
    {
        Identifier = NormalizeIdentifier(identifier);
        AppUserId = appUserId;
        StakeholderId = stakeholderId;
    }

    public string Identifier { get; private set; } = string.Empty;
    public Guid AppUserId { get; private set; }
    public Guid StakeholderId { get; private set; }

    public static DeviceAccount Create(string identifier, Guid appUserId, Guid stakeholderId) =>
        new(identifier, appUserId, stakeholderId);

    public static string NormalizeIdentifier(string identifier)
    {
        var normalized = identifier.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Device identifier is required.", nameof(identifier));
        }

        if (normalized.Length > MaxIdentifierLength)
        {
            throw new ArgumentException(
                $"Device identifier must not exceed {MaxIdentifierLength} characters.",
                nameof(identifier));
        }

        return normalized;
    }
}
